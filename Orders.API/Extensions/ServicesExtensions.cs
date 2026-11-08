using System.Reflection;
using Dapper;
using Microsoft.OpenApi.Models;
using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.ExceptionHandlers;
using Orders.API.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Orders.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<OrderRepository>();

        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();

        var usersApiUrl = config["Services:UsersApiUrl"] ?? "http://localhost:5112";
        var productsApiUrl = config["Services:ProductsApiUrl"] ?? "http://localhost:5110";

        services.AddHttpClient<UsersApiClient>(client =>
        {
            client.BaseAddress = new Uri(usersApiUrl);
        }).AddHttpMessageHandler<CorrelationIdHandler>();

        services.AddHttpClient<ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(productsApiUrl);
        }).AddHttpMessageHandler<CorrelationIdHandler>();

        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Vincula los códigos del catálogo en la tabla de respuestas
            options.OperationFilter<OrderOpenApiFilter>();

            // Garantiza que el contrato ErrorResponse figure visible en la sección Schemas al pie
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