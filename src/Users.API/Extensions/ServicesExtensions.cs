using System.Reflection;
using Dapper;
using ECommerce.Shared;
using Users.API.Data;
using Users.API.HealthChecks;
using Users.API.Services;

namespace Users.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<UserRepository>();
        services.AddScoped<UserService>();
        services.AddApiInfrastructure(config, new ApiProfile("Users.API", "USR-002", "USR-006"));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<UserOpenApiFilter>();
            var xml = Path.Combine(AppContext.BaseDirectory, "Users.API.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml);
        });
        services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["ready", "database"]);
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("UsersAPI", "/health");
        }).AddInMemoryStorage();
    }
}
