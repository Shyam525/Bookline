namespace Bookline.Application.Common.Interfaces;

using Bookline.Domain.Enums;

public interface INotificationService
{
    Task SendNotificationAsync(
        Guid tenantId,
        NotificationType type,
        NotificationChannel channel,
        string recipientEmail,
        string? recipientPhone,
        string subject,
        string body,
        Guid? bookingId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    Task ProcessUpcomingRemindersAsync(CancellationToken cancellationToken = default);
}
