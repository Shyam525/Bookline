using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Bookline.Api.Health;

/// <summary>
/// Section 118: Safe, bounded readiness health check verifying PostgreSQL database connectivity.
/// Executes bounded probe (SELECT 1) without large scans, analytics queries, or leaking secrets/stack traces.
/// </summary>
public class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostgreSqlHealthCheck> _logger;

    public PostgreSqlHealthCheck(IServiceScopeFactory scopeFactory, ILogger<PostgreSqlHealthCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Enforce strict bounded timeout (2000ms max)
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database connectivity verification failed.");
            }

            if (dbContext.Database.IsRelational())
            {
                // Lightweight safe probe: SELECT 1
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cts.Token);
            }

            return HealthCheckResult.Healthy("Database connectivity verified.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database readiness probe timed out after 2000ms.");
            return HealthCheckResult.Unhealthy("Database probe timed out.");
        }
        catch (Exception ex)
        {
            // Log full exception internally, but sanitize result to avoid leaking connection strings or credentials
            _logger.LogError(ex, "Database readiness probe encountered an internal error.");
            return HealthCheckResult.Unhealthy("Database connectivity check failed.");
        }
    }
}
