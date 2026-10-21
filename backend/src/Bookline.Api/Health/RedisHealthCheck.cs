using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Bookline.Api.Health;

/// <summary>
/// Section 118: Redis readiness health check.
/// Verifies Redis connectivity with bounded timeout (1000ms).
/// If Redis is optional (default in local/fallback mode), reports Degraded/Healthy
/// rather than failing all traffic unnecessarily.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<RedisHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isRedisRequired = string.Equals(_configuration["Redis:Required"], "true", StringComparison.OrdinalIgnoreCase);
        var redisMultiplexer = _serviceProvider.GetService<IConnectionMultiplexer>();

        if (redisMultiplexer == null)
        {
            if (isRedisRequired)
            {
                return HealthCheckResult.Unhealthy("Redis coordination engine is required by configuration but not initialized.");
            }

            return HealthCheckResult.Healthy("Operating in in-memory coordination mode (Redis optional).");
        }

        if (!redisMultiplexer.IsConnected)
        {
            if (isRedisRequired)
            {
                return HealthCheckResult.Unhealthy("Redis connection is not currently established.");
            }

            return HealthCheckResult.Degraded("Redis is disconnected; local in-memory fallback is active.");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(1)); // Bounded 1-second timeout

            var database = redisMultiplexer.GetDatabase();
            var ping = await database.PingAsync();

            return HealthCheckResult.Healthy($"Redis coordination engine connected (ping: {ping.TotalMilliseconds:F1}ms).");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Redis ping probe timed out.");
            if (isRedisRequired)
            {
                return HealthCheckResult.Unhealthy("Redis probe timed out.");
            }

            return HealthCheckResult.Degraded("Redis probe timed out; fallback in-memory coordination operational.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis probe failed.");
            if (isRedisRequired)
            {
                return HealthCheckResult.Unhealthy("Redis probe failed.");
            }

            return HealthCheckResult.Degraded("Redis probe failed; fallback in-memory coordination operational.");
        }
    }
}
