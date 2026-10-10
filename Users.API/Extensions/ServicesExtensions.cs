using System.Reflection;
using Dapper;
using Users.API.Data;
using Users.API.ExceptionHandlers;
using Users.API.HealthChecks;

namespace Users.API.Extensions;

public static class ServicesExtensions
{
    public static void AddAppServices(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<UserRepository>();

        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<UserOpenApiFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["database"])
            .AddCheck<ApiStatusCheck>("api-status", tags: ["api"]);

        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600);
            setup.AddHealthCheckEndpoint("UsersAPI", "/health");
        }).AddInMemoryStorage();
    }
}
