using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class TimeOff : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string Type { get; set; } = "Vacation"; // Vacation, Sick, Holiday, Personal, Custom
    public string? Reason { get; set; }

    public Staff Staff { get; set; } = null!;
}
