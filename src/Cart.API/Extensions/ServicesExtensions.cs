using System.Reflection;
using Dapper;
using ECommerce.Shared;
using Cart.API.Data;
using Cart.API.HealthChecks;
using Cart.API.Services;
using Cart.API.Clients;

namespace Cart.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<CartRepository>();
        services.AddScoped<CartService>();
        services.AddApiInfrastructure(config, new ApiProfile("Cart.API", "CRT-004", "CRT-005"), "Products");
        services.AddHttpClient<ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ProductsApiUrl"] ?? "http://localhost:5110");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ECommerce.Shared.CorrelationIdHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<CartOpenApiFilter>();
            var xml = Path.Combine(AppContext.BaseDirectory, "Cart.API.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml);
        });
        services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("CartAPI", "/health");
        }).AddInMemoryStorage();
    }
}
