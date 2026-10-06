namespace Bookline.Application.Notifications.DTOs;

public record NotificationSettingDto(
    Guid Id,
    Guid TenantId,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool Reminder24hEnabled,
    bool Reminder1hEnabled,
    string SenderEmail,
    string SenderName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record UpdateNotificationSettingDto(
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool Reminder24hEnabled,
    bool Reminder1hEnabled,
    string SenderEmail,
    string SenderName
);
