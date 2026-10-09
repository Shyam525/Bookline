using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class WorkingHours : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Label { get; set; }

    public Staff Staff { get; set; } = null!;
}
