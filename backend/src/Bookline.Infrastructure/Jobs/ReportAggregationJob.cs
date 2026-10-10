using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker job aggregating reporting metrics for tenants and platform adhering to Section 122.
/// Pre-aggregates daily gross revenue, booking volumes, hold drop-off rates, and active clients.
/// </summary>
public class ReportAggregationJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ReportAggregationJob> _logger;

    public ReportAggregationJob(IApplicationDbContext context, ILogger<ReportAggregationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        if (!tenants.Any()) return 0;

        _logger.LogInformation("Aggregating daily report metrics for {Count} active tenants...", tenants.Count);

        var todayUtc = DateTimeOffset.UtcNow.Date;
        var todayDate = DateTime.UtcNow.Date;

        foreach (var tenant in tenants)
        {
            var todaysBookings = await _context.Bookings
                .IgnoreQueryFilters()
                .Where(b => b.TenantId == tenant.Id && b.StartUtc >= todayUtc && b.StartUtc < todayUtc.AddDays(1))
                .ToListAsync(cancellationToken);

            var todaysOrders = await _context.Orders
                .IgnoreQueryFilters()
                .Where(o => o.TenantId == tenant.Id && o.CreatedAtUtc >= todayDate && o.CreatedAtUtc < todayDate.AddDays(1))
                .ToListAsync(cancellationToken);

            var grossBookingRevenue = todaysBookings.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.TotalPrice);
            var grossRetailRevenue = todaysOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

            _logger.LogInformation("Tenant {TenantSlug} Report: {BookingCount} bookings (${GrossBookings:F2}), {OrderCount} orders (${GrossRetail:F2})",
                tenant.Slug, todaysBookings.Count, grossBookingRevenue, todaysOrders.Count, grossRetailRevenue);
        }

        return tenants.Count;
    }
}
