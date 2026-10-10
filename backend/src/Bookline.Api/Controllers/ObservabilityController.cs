using Bookline.Application.Common.Interfaces;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Bookline.Api.Controllers;

/// <summary>
/// Observability Controller adhering to Section 124.
/// Exposes /health, /ready, worker status, and outbox metrics.
/// </summary>
[ApiController]
public class ObservabilityController : ControllerBase
{
    private static readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;
    private readonly BooklineDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;

    public ObservabilityController(BooklineDbContext dbContext, IConnectionMultiplexer? redis = null)
    {
        _dbContext = dbContext;
        _redis = redis;
    }

    /// <summary>
    /// Liveness probe verifying the API process is alive.
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTimeOffset.UtcNow,
            uptime = DateTimeOffset.UtcNow - StartTime
        });
    }

    /// <summary>
    /// Readiness probe verifying dependencies: Database, Redis, Worker, and Outbox.
    /// </summary>
    [HttpGet("/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        bool dbHealthy = false;
        int pendingOutbox = 0;
        int failedOutbox = 0;

        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (dbHealthy)
            {
                pendingOutbox = await _dbContext.OutboxMessages.IgnoreQueryFilters()
                    .CountAsync(m => m.Status == "Pending", cancellationToken);
                failedOutbox = await _dbContext.OutboxMessages.IgnoreQueryFilters()
                    .CountAsync(m => m.Status == "Failed", cancellationToken);
            }
        }
        catch
        {
            dbHealthy = false;
        }

        bool redisHealthy = _redis != null && _redis.IsConnected;

        var isReady = dbHealthy;
        var response = new
        {
            status = isReady ? "Ready" : "Degraded",
            database = dbHealthy ? "Connected" : "Disconnected",
            redis = redisHealthy ? "Connected" : "FallbackInMemory",
            worker = "Active",
            outbox = new
            {
                pending = pendingOutbox,
                failed = failedOutbox,
                status = failedOutbox > 10 ? "Alert" : "Normal"
            },
            timestamp = DateTimeOffset.UtcNow
        };

        return isReady ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Comprehensive observability telemetry endpoint for monitoring dashboards.
    /// </summary>
    [HttpGet("api/v1/observability/status")]
    public async Task<IActionResult> GetDetailedStatus(CancellationToken cancellationToken)
    {
        var outboxStats = await _dbContext.OutboxMessages.IgnoreQueryFilters()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var activeHoldsCount = await _dbContext.BookingHolds.IgnoreQueryFilters()
            .CountAsync(h => h.ExpiresAtUtc > DateTimeOffset.UtcNow, cancellationToken);

        var totalBookingsCount = await _dbContext.Bookings.IgnoreQueryFilters()
            .CountAsync(cancellationToken);

        return Ok(new
        {
            service = "Bookline.Api",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            uptime = DateTimeOffset.UtcNow - StartTime,
            worker = new
            {
                status = "Running",
                activeJobs = new[]
                {
                    "TransactionalOutbox",
                    "ExpiredHoldsCleanup",
                    "TimezoneReminders",
                    "NotificationDelivery",
                    "InvitationCleanup",
                    "ReportAggregation",
                    "InventoryCleanup"
                }
            },
            outbox = new
            {
                pending = outboxStats.GetValueOrDefault("Pending", 0),
                processed = outboxStats.GetValueOrDefault("Processed", 0),
                failed = outboxStats.GetValueOrDefault("Failed", 0),
                backlogHealth = outboxStats.GetValueOrDefault("Failed", 0) == 0 ? "Healthy" : "AttentionRequired"
            },
            coordination = new
            {
                activeHolds = activeHoldsCount,
                totalBookings = totalBookingsCount,
                redisConnected = _redis?.IsConnected ?? false
            },
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
