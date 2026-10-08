using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Product : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0.00m;
    public string Currency { get; set; } = "USD";
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    public int SoldQuantity { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsPurchasableOnline { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int AvailableQuantity => Math.Max(0, StockQuantity - ReservedQuantity);

    public bool Reserve(int quantity)
    {
        if (quantity <= 0 || AvailableQuantity < quantity)
        {
            return false;
        }

        ReservedQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0) return;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool Purchase(int quantity)
    {
        if (quantity <= 0) return false;

        if (ReservedQuantity >= quantity)
        {
            ReservedQuantity -= quantity;
            StockQuantity = Math.Max(0, StockQuantity - quantity);
            SoldQuantity += quantity;
            UpdatedAtUtc = DateTime.UtcNow;
            return true;
        }

        if (AvailableQuantity >= quantity)
        {
            StockQuantity = Math.Max(0, StockQuantity - quantity);
            SoldQuantity += quantity;
            UpdatedAtUtc = DateTime.UtcNow;
            return true;
        }

        return false;
    }
}
