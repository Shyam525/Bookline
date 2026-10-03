using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Invitation : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsAccepted { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
