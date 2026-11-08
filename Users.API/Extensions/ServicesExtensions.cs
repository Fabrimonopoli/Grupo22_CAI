using System.Reflection;
using Dapper;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Users.API.Data;
using Users.API.DTOs;
using Users.API.ExceptionHandlers;
using Users.API.HealthChecks;

namespace Users.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<UserRepository>();

        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Vincula los códigos del catálogo en la tabla de respuestas
            options.OperationFilter<UserOpenApiFilter>();

            // Expone el contrato ErrorResponse en la sección Schemas al pie
            options.DocumentFilter<ErrorResponseSchemaFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

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