namespace Bookline.Application.Products.DTOs;

public record ProductCategoryDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string Slug,
    string? Description,
    int SortOrder,
    bool IsActive,
    int ProductsCount
);

public record CreateProductCategoryRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    int SortOrder = 0
);

public record UpdateProductCategoryRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    int SortOrder = 0,
    bool IsActive = true
);

public record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    DateTime CreatedAtUtc
);

public record ProductImageInput(
    string Url,
    string? AltText = null,
    int SortOrder = 0,
    bool IsPrimary = false
);

public record ProductDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string Currency,
    string? Sku,
    Guid? CategoryId,
    string? CategoryName,
    IReadOnlyList<ProductImageDto> Images,
    string? ImageUrl,
    bool TrackInventory,
    int StockQuantity,
    int ReservedQuantity,
    int SoldQuantity,
    int AvailableQuantity,
    int LowStockThreshold,
    bool AllowBackorders,
    bool IsInStock,
    bool IsLowStock,
    bool IsActive,
    bool IsPurchasableOnline,
    bool IsPublished,
    string InventoryStatus,
    string CommercialStatus,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateProductRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    decimal Price = 0.00m,
    string? Currency = "USD",
    string? Sku = null,
    Guid? CategoryId = null,
    List<ProductImageInput>? Images = null,
    string? ImageUrl = null,
    bool TrackInventory = true,
    int StockQuantity = 0,
    int LowStockThreshold = 5,
    bool IsPurchasableOnline = true,
    bool IsPublished = true
);

public record UpdateProductRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    decimal Price = 0.00m,
    string? Currency = "USD",
    string? Sku = null,
    Guid? CategoryId = null,
    List<ProductImageInput>? Images = null,
    string? ImageUrl = null,
    bool TrackInventory = true,
    int StockQuantity = 0,
    int LowStockThreshold = 5,
    bool IsPurchasableOnline = true,
    bool IsPublished = true,
    bool IsActive = true
);

public record AdjustInventoryRequest(
    int NewStockQuantity,
    string? Reason = null
);

public record SetProductStatusRequest(
    bool? IsActive = null,
    bool? IsPublished = null,
    bool? IsPurchasableOnline = null
);
