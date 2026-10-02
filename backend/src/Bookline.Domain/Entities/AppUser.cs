using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class AppUser : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff"; // Owner, Staff, Receptionist
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
