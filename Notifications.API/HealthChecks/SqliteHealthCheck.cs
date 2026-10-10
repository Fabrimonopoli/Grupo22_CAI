using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notifications.API.HealthChecks;

public sealed class SqliteHealthCheck(IConfiguration config) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqliteConnection(config.GetConnectionString("DefaultConnection")
                ?? "Data Source=notifications.db;Foreign Keys=True;");
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("Base de datos SQLite respondiendo correctamente.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al conectar con SQLite.", ex);
        }
    }
}
