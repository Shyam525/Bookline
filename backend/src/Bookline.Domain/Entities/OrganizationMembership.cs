using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class OrganizationMembership : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Staff"; // Owner, Admin, Manager, Receptionist, Staff
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
