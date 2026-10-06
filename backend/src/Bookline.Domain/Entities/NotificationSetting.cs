using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class NotificationSetting : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
    public bool Reminder24hEnabled { get; set; } = true;
    public bool Reminder1hEnabled { get; set; } = true;
    public string SenderEmail { get; set; } = "no-reply@bookline.io";
    public string SenderName { get; set; } = "Bookline Appointments";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
