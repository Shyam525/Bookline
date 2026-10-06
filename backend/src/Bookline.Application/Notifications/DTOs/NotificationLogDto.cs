namespace Bookline.Application.Notifications.DTOs;

public record NotificationLogDto(
    Guid Id,
    Guid TenantId,
    Guid? BookingId,
    Guid? CustomerId,
    string RecipientEmail,
    string? RecipientPhone,
    string NotificationType,
    string Channel,
    string Status,
    string Subject,
    string Body,
    string? ErrorMessage,
    DateTime? SentAtUtc,
    DateTime CreatedAtUtc
);
