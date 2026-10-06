using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.API.HealthChecks;

public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    public SqliteHealthCheck(IConfiguration config) => _config = config;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _config.GetConnectionString("DefaultConnection") ?? "Data Source=users.db;Foreign Keys=True;";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("Base de datos SQLite respondiendo correctamente.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al conectar con la base de datos SQLite.", ex);
        }
    }
}
