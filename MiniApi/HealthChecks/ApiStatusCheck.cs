using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MiniApi.HealthChecks
{
    public class ApiStatusCheck : IHealthCheck
    {
        private static readonly DateTime StartTime = DateTime.UtcNow;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var uptime = DateTime.UtcNow - StartTime;
            var version = Environment.Version.ToString();

            var data = new Dictionary<string, object>
            {
                ["runtime"] = $".NET {version}",
                ["uptime"] = uptime.ToString(@"hh\:mm\:ss"),
                ["startedAt"] = StartTime.ToString("o")
            };

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    description: $"API operativa — .NET {version}",
                    data: data));
        }
    }
}