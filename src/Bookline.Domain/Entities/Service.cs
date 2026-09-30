using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Service : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
