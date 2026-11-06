using System.Reflection;
using Dapper;
using Products.API.Clients;
using Products.API.Data;
using Products.API.ExceptionHandlers;
using Products.API.HealthChecks;

namespace Products.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<ProductRepository>();

        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();

        var ordersApiUrl = config["Services:OrdersApiUrl"] ?? "http://localhost:5114";
        services.AddHttpClient<OrdersApiClient>(client =>
        {
            client.BaseAddress = new Uri(ordersApiUrl);
        }).AddHttpMessageHandler<CorrelationIdHandler>();

        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
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