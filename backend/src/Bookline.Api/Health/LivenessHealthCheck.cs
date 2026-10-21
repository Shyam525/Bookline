using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bookline.Api.Health;

/// <summary>
/// Section 118: Liveness probe verifying application process is alive and able to respond to basic requests.
/// Does not depend on downstream services to prevent restart loops.
/// </summary>
public class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Bookline application process is alive and responsive."));
    }
}
