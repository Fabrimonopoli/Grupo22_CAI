using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Products.API.Middleware;
using Serilog;

namespace Products.API.Extensions;

public static class MiddlewareExtensions
{
    public static void UseAppMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, ex) =>
                (ex != null) ? Serilog.Events.LogEventLevel.Error :
                    (httpContext.Request.Path.StartsWithSegments("/health"))
                        ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information;
        });

        app.UseMiddleware<AuditMiddleware>();
        app.UseExceptionHandler();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");
    }
}
