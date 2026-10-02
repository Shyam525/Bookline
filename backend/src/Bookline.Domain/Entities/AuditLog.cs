namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;

public class AuditLog : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
