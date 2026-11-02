using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace ECommerce.Shared;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public static string GetId(HttpContext context) => context.Items["CorrelationId"] as string ?? context.TraceIdentifier;
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var id = !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 100
            && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')
            ? incoming : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = id;
        context.Request.Headers["X-Correlation-Id"] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-Id"] = id;
            return Task.CompletedTask;
        });
        using (LogContext.PushProperty("CorrelationId", id))
        using (LogContext.PushProperty("Endpoint", context.Request.Path.Value))
            await next(context);
    }
}

/// <summary>Auditoría de requests; no intercepta ni reemplaza el manejo de excepciones.</summary>
public sealed class RequestAuditMiddleware(RequestDelegate next, ILogger<RequestAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Inicio {RequestMethod} {Endpoint}", context.Request.Method, context.Request.Path.Value);
        try
        {
            await next(context);
        }
        finally
        {
            var level = context.Response.StatusCode >= 500 ? LogLevel.Error :
                context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(level, "Fin {RequestMethod} {Endpoint} {StatusCode} en {DurationMs} ms {ErrorCode}",
                context.Request.Method, context.Request.Path.Value, context.Response.StatusCode,
                timer.Elapsed.TotalMilliseconds, context.Items["ErrorCode"] as string);
        }
    }
}

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var id = accessor.HttpContext is { } context
            ? CorrelationIdMiddleware.GetId(context) : Guid.NewGuid().ToString("N");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", id);
        return base.SendAsync(request, cancellationToken);
    }
}

public sealed class DependencyHealthCheck(IHttpClientFactory factory, Uri baseUri) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await factory.CreateClient("readiness").GetAsync(new Uri(baseUri, "/health/local"), cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Dependencia no disponible.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("No se pudo consultar la dependencia.");
        }
    }
}

public static class ApiInfrastructure
{
    public static void AddApiLogging(this WebApplicationBuilder builder, string service)
    {
        var settings = builder.Configuration;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Enum.TryParse<LogEventLevel>(settings["Observability:MinimumLevel"], true, out var level) ? level : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext().Enrich.WithProperty("Servicio", service)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(new JsonFormatter(renderMessage: true), settings["Observability:LogPath"] ?? "logs/audit-.jsonl",
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();
        builder.Host.UseSerilog();
    }

    public static void AddApiInfrastructure(this IServiceCollection services, IConfiguration config, ApiProfile profile,
        params string[] dependencyNames)
    {
        services.AddSingleton(profile);
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = action => throw new System.ComponentModel.DataAnnotations.ValidationException(
                string.Join("; ", action.ModelState.Values.SelectMany(value => value.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El formato de los datos es inválido." : error.ErrorMessage)));
        });
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        services.AddProblemDetails();
        services.AddHttpClient("readiness", client => client.Timeout = TimeSpan.FromSeconds(3))
            .AddHttpMessageHandler<CorrelationIdHandler>();
        var health = services.AddHealthChecks().AddCheck("process", () => HealthCheckResult.Healthy(), tags: ["live"]);
        foreach (var name in dependencyNames)
        {
            var url = config[$"Services:{name}ApiUrl"] ?? throw new InvalidOperationException($"Falta Services:{name}ApiUrl");
            health.Add(new HealthCheckRegistration($"dependency-{name.ToLowerInvariant()}",
                sp => new DependencyHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), new Uri(url)),
                HealthStatus.Unhealthy, ["ready"]));
        }
    }

    public static void UseApiInfrastructure(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestAuditMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(async statusContext =>
        {
            var ctx = statusContext.HttpContext;
            var status = ctx.Response.StatusCode;
            var profile = ctx.RequestServices.GetRequiredService<ApiProfile>();
            var code = status == 400 ? profile.ValidationCode : $"HTTP-{status}";
            var message = status == 404 ? "La ruta o el recurso solicitado no fue encontrado." :
                status == 405 ? "El método HTTP no está permitido para esta ruta." :
                ErrorCatalog.Entries.GetValueOrDefault(code).Message ?? "La solicitud no pudo procesarse.";
            await ApiErrors.WriteAsync(ctx, status, code, message);
        });
        if (app.Configuration.GetValue("Swagger:Enabled", true))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        static Task WriteHealth(HttpContext context, HealthReport report)
            => context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                entries = report.Entries.ToDictionary(x => x.Key, x => new
                {
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    duration = x.Value.Duration.ToString()
                })
            });
        app.MapHealthChecks("/health", new()
        {
            ResponseWriter = WriteHealth
        });
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => !check.Tags.Contains("live"),
            ResponseWriter = WriteHealth
        });
        app.MapHealthChecks("/health/live", new()
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealth
        });
        // Proceso y almacenamiento propios; las dependencias consultan esta ruta sin crear ciclos.
        app.MapHealthChecks("/health/local", new()
        {
            Predicate = check => !check.Name.StartsWith("dependency-", StringComparison.Ordinal),
            ResponseWriter = WriteHealth
        });
    }
}
