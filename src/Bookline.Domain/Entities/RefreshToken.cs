using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class RefreshToken : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty; // Hashed with SHA-256
    public DateTime ExpiryUtc { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
