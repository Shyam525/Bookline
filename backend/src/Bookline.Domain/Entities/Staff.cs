using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Staff : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Title { get; set; } = "Staff Member";
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<TimeOff> TimeOffs { get; set; } = new List<TimeOff>();
}
