using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class ProviderActivityEvent : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public bool IsAutomatedOrTest { get; set; } = false;
}

