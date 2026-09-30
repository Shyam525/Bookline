namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;

public class WebhookDeliveryLog : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ResponsePayload { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
