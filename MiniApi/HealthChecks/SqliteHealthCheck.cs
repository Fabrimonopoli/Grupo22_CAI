using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MiniApi.HealthChecks
{
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        public SqliteHealthCheck(IConfiguration config) => _config = config;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var connectionString = _config.GetConnectionString("DefaultConnection")
                    ?? "Data Source=app.db";

                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                // Consulta mínima — solo verifica que SQLite responde
                await conn.ExecuteScalarAsync<int>("SELECT 1");

                return HealthCheckResult.Healthy("SELECT 1 ejecutado OK");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    description: "No se pudo conectar a SQLite",
                    exception: ex);
            }
        }
    }
}
