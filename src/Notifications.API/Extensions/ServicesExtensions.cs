using System.Reflection;
using Dapper;
using ECommerce.Shared;
using Notifications.API.Data;
using Notifications.API.HealthChecks;
using Notifications.API.Services;
using Notifications.API.Clients;

namespace Notifications.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<NotificationRepository>();
        services.AddScoped<NotificationService>();
        services.AddApiInfrastructure(config, new ApiProfile("Notifications.API", "NTF-002", "NTF-004"), "Users");
        services.AddHttpClient<UsersApiClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:UsersApiUrl"] ?? "http://localhost:5112");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ECommerce.Shared.CorrelationIdHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<NotificationOpenApiFilter>();
            var xml = Path.Combine(AppContext.BaseDirectory, "Notifications.API.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml);
        });
        services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("NotificationsAPI", "/health");
        }).AddInMemoryStorage();
    }
}
