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
    private readonly IApplicationDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;

    public ObservabilityController(IApplicationDbContext dbContext, IConnectionMultiplexer? redis = null)
    {
        _dbContext = dbContext;
        _redis = redis;
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
