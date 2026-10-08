using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Location : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Ahmedabad";
    public string State { get; set; } = "Gujarat";
    public string Country { get; set; } = "India";
    public string PostalCode { get; set; } = string.Empty;
    public double Latitude { get; set; } = 23.0225;
    public double Longitude { get; set; } = 72.5714;
    public string Phone { get; set; } = string.Empty;
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
