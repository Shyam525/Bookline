using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class StorefrontFunnelEvent : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Step { get; set; } = string.Empty; // Visitor, AvailabilityViewed, HoldCreated, BookingConfirmed
    public string VisitorSessionId { get; set; } = string.Empty;
    public Guid? RelatedHoldId { get; set; }
    public Guid? RelatedBookingId { get; set; }
    public bool IsAutomatedOrBot { get; set; } = false;
    public bool IsInternalPreview { get; set; } = false;
    public string DateKey { get; set; } = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}

