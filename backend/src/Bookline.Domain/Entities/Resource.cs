using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Resource : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Room"; // Room, Chair, Machine, Treatment room
    public int Capacity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
