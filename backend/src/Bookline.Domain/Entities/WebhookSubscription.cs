namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;

public class WebhookSubscription : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string EventTypes { get; set; } = "*";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
