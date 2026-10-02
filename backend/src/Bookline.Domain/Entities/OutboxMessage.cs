namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;

public class OutboxMessage : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
