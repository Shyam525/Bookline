using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class ProductImage : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
