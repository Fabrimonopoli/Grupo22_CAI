using System.Reflection;
using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.ExceptionHandlers;
using Notifications.API.HealthChecks;

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
            var xmlPath = Path.Combine(AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        });

        services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["database"])
            .AddCheck<ApiStatusCheck>("api-status", tags: ["api"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("NotificationsAPI", "/health");
        }).AddInMemoryStorage();
    }
}
