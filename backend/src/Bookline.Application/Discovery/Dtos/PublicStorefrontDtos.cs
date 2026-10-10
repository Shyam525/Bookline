namespace Bookline.Application.Discovery.Dtos;

/// <summary>
/// Public provider profile DTO adhering to Section 112 & 113.
/// Strictly excludes private operational/financial details (commissions, payouts, internal notes).
/// </summary>
public record PublicProviderProfileDto(
    Guid Id,
    string Name,
    string Slug,
    string Category,
    string? BusinessType,
    string? Description,
    string? Address,
    string City,
    string? State,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? Phone,
    string? Website,
    double AverageRating,
    int ReviewCount,
    string? LogoUrl,
    string? CoverImageUrl,
    string[] GalleryUrls,
    bool IsVerified,
    string DepositType,
    decimal DepositAmount,
    string Currency,
    int HoldDurationMinutes,
    int MinimumNoticeHours
);

public record PublicLocationDto(
    Guid Id,
    string Name,
    string Address,
    string City,
    string? State,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? Phone,
    bool IsActive
);

public record PublicServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description
);

public record PublicServiceDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price,
    string Currency,
    bool IsOnlineBookingEnabled
);

public record PublicProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? Sku,
    string? ImageUrl,
    bool IsInStock
);

public record PublicStaffDto(
    Guid Id,
    string Name,
    string? Title,
    string? Bio,
    string? AvatarUrl
);

public record PublicReviewDto(
    Guid Id,
    string CustomerName,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAtUtc
);

public record PublicStorefrontResponse(
    PublicProviderProfileDto Provider,
    IReadOnlyList<PublicLocationDto> Locations,
    IReadOnlyList<PublicServiceCategoryDto> Categories,
    IReadOnlyList<PublicServiceDto> Services,
    IReadOnlyList<PublicProductDto> Products,
    IReadOnlyList<PublicStaffDto> Staff,
    IReadOnlyList<PublicReviewDto> Reviews
);
