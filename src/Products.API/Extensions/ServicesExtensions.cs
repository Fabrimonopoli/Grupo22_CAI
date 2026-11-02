using System.Reflection;
using Dapper;
using ECommerce.Shared;
using Products.API.Data;
using Products.API.HealthChecks;
using Products.API.Services;
using Products.API.Clients;

namespace Products.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<StockReservationRepository>();
        services.AddApiInfrastructure(config, new ApiProfile("Products.API", "PRD-002", "PRD-005"), "Orders");
        services.AddHttpClient<OrdersApiClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:OrdersApiUrl"] ?? "http://localhost:5114");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ECommerce.Shared.CorrelationIdHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<ProductOpenApiFilter>();
            var xml = Path.Combine(AppContext.BaseDirectory, "Products.API.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml);
        });
        services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("ProductsAPI", "/health");
        }).AddInMemoryStorage();
    }
}
