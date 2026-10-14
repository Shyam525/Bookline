using System.ComponentModel.DataAnnotations.Schema;
using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Product : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [NotMapped]
    public Guid BusinessId
    {
        get => TenantId;
        set => TenantId = value;
    }

    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0.00m;
    public string Currency { get; set; } = "USD";
    public string? Sku { get; set; }
    
    private string? _imageUrl;
    public string? ImageUrl
    {
        get => _imageUrl ?? Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? Images.FirstOrDefault()?.Url;
        set => _imageUrl = value;
    }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    // Inventory Tracking & Policy
    public bool TrackInventory { get; set; } = true;
    public int LowStockThreshold { get; set; } = 5;
    public bool AllowBackorders { get; set; } = false;
    public int StockQuantity { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    public int SoldQuantity { get; set; } = 0;

    // Status & Publication
    public bool IsActive { get; set; } = true;
    public bool IsPurchasableOnline { get; set; } = true;
    public bool IsPublished { get; set; } = true;
    public Enums.ModerationStatus ModerationStatus { get; set; } = Enums.ModerationStatus.Approved;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int AvailableQuantity => TrackInventory ? Math.Max(0, StockQuantity - ReservedQuantity) : int.MaxValue;
    public bool IsInStock => !TrackInventory || AvailableQuantity > 0;
    public bool IsLowStock => TrackInventory && AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;

    public bool Reserve(int quantity)
    {
        if (quantity <= 0) return false;
        if (TrackInventory && AvailableQuantity < quantity) return false;

        if (TrackInventory)
        {
            ReservedQuantity += quantity;
        }
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0) return;
        if (TrackInventory)
        {
            ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool Purchase(int quantity)
    {
        if (quantity <= 0) return false;

        if (!TrackInventory)
        {
            SoldQuantity += quantity;
            UpdatedAtUtc = DateTime.UtcNow;
            return true;
        }

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

    public bool Restock(int quantity)
    {
        if (quantity <= 0) return false;
        StockQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    public bool AdjustStock(int newStockQuantity)
    {
        if (newStockQuantity < 0) return false; // Strict non-negative inventory invariant
        StockQuantity = newStockQuantity;
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }
}
