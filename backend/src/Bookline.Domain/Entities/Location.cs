using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Location : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
