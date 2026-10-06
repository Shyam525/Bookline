using Bookline.Domain.Common;
using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class NotificationLog : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BookingId { get; set; }
    public Guid? CustomerId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public NotificationType NotificationType { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
