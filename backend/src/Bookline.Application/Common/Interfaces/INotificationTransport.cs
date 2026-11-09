namespace Bookline.Application.Common.Interfaces;

using Bookline.Domain.Enums;

public record NotificationDeliveryRequest(
    Guid TenantId,
    NotificationChannel Channel,
    string Recipient,
    string? RecipientName,
    string Subject,
    string Body,
    Guid? BookingId = null,
    Guid? CustomerId = null,
    string? DeepLinkUrl = null,
    string? MetadataJson = null
);

public record NotificationDeliveryResult(
    bool Success,
    string? ExternalMessageId = null,
    string? ErrorMessage = null,
    DateTimeOffset TimestampUtc = default
)
{
    public static NotificationDeliveryResult Succeeded(string? messageId = null) =>
        new(true, messageId, null, DateTimeOffset.UtcNow);

    public static NotificationDeliveryResult Failed(string error) =>
        new(false, null, error, DateTimeOffset.UtcNow);
}

/// <summary>
/// Provider abstraction for notification transport, allowing SMS, Email, or WhatsApp
/// adapters to be swapped without rewriting appointment or domain logic.
/// </summary>
public interface INotificationTransport
{
    string TransportName { get; }
    bool CanHandle(NotificationChannel channel);
    Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default);
}

public interface INotificationTransportDispatcher
{
    Task<NotificationDeliveryResult> DispatchAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default);
}

