namespace Bookline.Infrastructure.Jobs;

using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class CleanupStalePendingBookingsJob
{
    private readonly IApplicationDbContext _context;

    public CleanupStalePendingBookingsJob(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-15);

        var staleBookings = await _context.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAtUtc <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var booking in staleBookings)
        {
            booking.Cancel();
        }

        if (staleBookings.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
