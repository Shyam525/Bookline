using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker job releasing expired slot holds adhering to Section 58 & 122.
/// Releases holds past expiration time so slots return to free availability.
/// </summary>
public class ExpiredHoldsCleanupJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ExpiredHoldsCleanupJob> _logger;

    public ExpiredHoldsCleanupJob(IApplicationDbContext context, ILogger<ExpiredHoldsCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredHolds = await _context.BookingHolds
            .IgnoreQueryFilters()
            .Where(h => h.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (!expiredHolds.Any()) return 0;

        _logger.LogInformation("Cleaning up {Count} expired booking holds...", expiredHolds.Count);

        _context.BookingHolds.RemoveRange(expiredHolds);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredHolds.Count;
    }
}
