using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class ServiceLocation : TenantEntity
{
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public bool IsAvailable { get; set; } = true;
    public decimal? PriceOverride { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
