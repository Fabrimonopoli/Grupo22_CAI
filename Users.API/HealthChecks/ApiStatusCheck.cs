using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.API.HealthChecks;

public class ApiStatusCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.FromResult(HealthCheckResult.Healthy("Users.API se encuentra operativa."));
}
