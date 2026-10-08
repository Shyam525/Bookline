namespace Bookline.Application.Common.Interfaces;

public record ProviderSearchQuery(
    string? Query = null,
    string? Category = null,
    string? Service = null,
    string? City = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusKm = null,
    double? SwLat = null,
    double? SwLng = null,
    double? NeLat = null,
    double? NeLng = null,
    double? MinRating = null,
    decimal? MaxPrice = null,
    string? Availability = null, // "today", "tomorrow", "this-week"
    string? Sort = "Recommended", // "Recommended", "Nearest", "Top rated", "Most reviewed", "Earliest availability", "Lowest price", "Highest price"
    int Page = 1,
    int PageSize = 12
);

public record ProviderCardDto(
    Guid Id,
    string Name,
    string Slug,
    string Category,
    string BusinessType,
    double Rating,
    int ReviewCount,
    double? DistanceKm,
    string City,
    string Address,
    double Latitude,
    double Longitude,
    decimal StartingPrice,
    string Currency,
    string? NextAvailableSlot,
    bool IsVerified,
    string? LogoUrl,
    string? CoverImageUrl,
    List<string> ServicesSummary,
    double RankingScore
);

public record ProviderSearchResponse(
    List<ProviderCardDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public interface IProviderSearchService
{
    Task<ProviderSearchResponse> SearchProvidersAsync(ProviderSearchQuery query, CancellationToken cancellationToken = default);
}
