namespace Bookline.Domain.Entities;

/// <summary>
/// Idempotency ledger record adhering to Section 121.
/// Guarantees critical operations (booking, payment, refund, order, webhook, notification)
/// are safely repeatable without duplicate financial or scheduling side effects.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? RequestHash { get; set; }
    public int StatusCode { get; set; }
    public string ResponseJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
}
