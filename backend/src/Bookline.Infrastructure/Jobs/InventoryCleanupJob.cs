using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker job maintaining inventory concurrency adhering to Section 119 & 122.
/// Detects abandoned checkout attempts older than 30 minutes and restores reserved product stocks.
/// </summary>
public class InventoryCleanupJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InventoryCleanupJob> _logger;

    public InventoryCleanupJob(IApplicationDbContext context, ILogger<InventoryCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);

        // Find stale pending orders that were never paid or confirmed
        var abandonedOrders = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAtUtc <= cutoff)
            .ToListAsync(cancellationToken);

        if (!abandonedOrders.Any()) return 0;

        _logger.LogInformation("Reclaiming reserved inventory from {Count} abandoned orders...", abandonedOrders.Count);

        foreach (var order in abandonedOrders)
        {
            foreach (var item in order.Items)
            {
                var product = await _context.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

                if (product != null)
                {
                    // Restore stock quantity
                    product.StockQuantity += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return abandonedOrders.Count;
    }
}
