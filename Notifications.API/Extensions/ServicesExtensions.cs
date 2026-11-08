using System.Reflection;
using Microsoft.OpenApi.Models;
using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.ExceptionHandlers;
using Notifications.API.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notifications.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<NotificationRepository>();
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();

        var usersApiUrl = config["Services:UsersApiUrl"] ?? "http://localhost:5112";
        services.AddHttpClient<UsersApiClient>(client => client.BaseAddress = new Uri(usersApiUrl))
            .AddHttpMessageHandler<CorrelationIdHandler>();

        services.AddExceptionHandler<NotificationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Vincula los códigos del catálogo NTF-XXX en la tabla de respuestas
            options.OperationFilter<NotificationOpenApiFilter>();

            // Expone el contrato ErrorResponse en la sección Schemas al pie
            options.DocumentFilter<ErrorResponseSchemaFilter>();

            var xmlPath = Path.Combine(AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        });

        // Health Checks con tags 'ready' y 'live' requeridos
        services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"])
            .AddCheck<ApiStatusCheck>("api-status", tags: ["live", "api"]);
    }
}

public class ErrorResponseSchemaFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository);
    }
}