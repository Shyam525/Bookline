using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Staff : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<TimeOff> TimeOffs { get; set; } = new List<TimeOff>();
}
