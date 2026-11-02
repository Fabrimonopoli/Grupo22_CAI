using System.Reflection;
using Dapper;
using ECommerce.Shared;
using Orders.API.Data;
using Orders.API.HealthChecks;
using Orders.API.Services;
using Orders.API.Clients;

namespace Orders.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<OrderRepository>();
        services.AddScoped<OrderService>();
        services.AddHostedService<OrderRecoveryWorker>();
        services.AddApiInfrastructure(config, new ApiProfile("Orders.API", "ORD-002", "ORD-007"), "Users", "Products");
        services.AddHttpClient<UsersApiClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:UsersApiUrl"] ?? "http://localhost:5112");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ECommerce.Shared.CorrelationIdHandler>();
        services.AddHttpClient<ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ProductsApiUrl"] ?? "http://localhost:5110");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ECommerce.Shared.CorrelationIdHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<OrderOpenApiFilter>();
            var xml = Path.Combine(AppContext.BaseDirectory, "Orders.API.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml);
        });
        services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("OrdersAPI", "/health");
        }).AddInMemoryStorage();
    }
}
