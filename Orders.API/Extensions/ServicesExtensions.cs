using System.Reflection;
using Dapper;
using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.ExceptionHandlers;
using Orders.API.HealthChecks;

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
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // Health Checks con tags 'ready' y 'live' requeridos
        services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"])
            .AddCheck<ApiStatusCheck>("api-status", tags: ["live", "api"]);
    }
}