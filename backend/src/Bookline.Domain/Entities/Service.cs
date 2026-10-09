using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Service : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public int BufferBeforeMinutes { get; set; } = 0;
    public int BufferAfterMinutes { get; set; } = 0;
    public decimal Price { get; set; } = 0.00m;
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsOnlineBookingEnabled { get; set; } = true;
    public bool IsArchived { get; set; } = false;
    public string? ColorHex { get; set; } = "#E8546A";
    public bool RequiresResource { get; set; } = false;
    public string? RequiredResourceType { get; set; } = null; // Room, Chair, Machine, Treatment room
    public string? ImageUrl { get; set; }
    public Enums.ModerationStatus ModerationStatus { get; set; } = Enums.ModerationStatus.Approved;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int TotalDurationMinutes => DurationMinutes + BufferBeforeMinutes + BufferAfterMinutes;
    public int BufferMinutes { get => BufferBeforeMinutes + BufferAfterMinutes; set { BufferBeforeMinutes = value; } }
}
