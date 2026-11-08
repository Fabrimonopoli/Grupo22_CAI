using System.Reflection;
using Cart.API.Clients;
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.ExceptionHandlers;
using Cart.API.HealthChecks;
using Dapper;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cart.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
            
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();

        var productsApiUrl = config["Services:ProductsApiUrl"] ?? "http://localhost:5110";
        services.AddHttpClient<ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(productsApiUrl);
        }).AddHttpMessageHandler<CorrelationIdHandler>();

        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<CartRepository>();

        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Vincula los códigos del catálogo CRT-XXX en la tabla de respuestas
            options.OperationFilter<CartOpenApiFilter>();

            // Expone el contrato ErrorResponse en la sección Schemas al pie
            options.DocumentFilter<ErrorResponseSchemaFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // Health Checks con tags obligatorios 'ready' y 'live'
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