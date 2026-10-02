using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class StaffService : TenantEntity
{
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
