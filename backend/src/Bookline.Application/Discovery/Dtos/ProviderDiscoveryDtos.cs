using Bookline.Domain.Enums;

namespace Bookline.Application.Discovery.Dtos;

public enum DiscoverySortMode
{
    Recommended = 1,
    Nearest = 2,
    TopRated = 3,
    MostReviewed = 4,
    EarliestAvailable = 5,
    LowestPrice = 6,
    HighestPrice = 7
}

public record DiscoveryQuery(
    string? QueryText = null,
    string? CategorySlug = null,
    string? ServiceSlug = null,
    PlaceSuggestionDto? SelectedPlace = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusMeters = null,
    ViewportBounds? ViewportBounds = null,
    double? MinimumRating = null,
    decimal? MaximumPrice = null,
    DateOnly? AvailabilityDate = null,
    DiscoverySortMode SortMode = DiscoverySortMode.Recommended,
    int Page = 1,
    int PageSize = 12,
    bool IncludeMarkers = false
);

public record ProviderLocationResult(
    Guid BusinessId,
    string BusinessSlug,
    Guid LocationId,
    string PublicName,
    string Category,
    string? PublicImage,
    string PublicAddress,
    string City,
    string? Area,
    double Latitude,
    double Longitude,
    double? DistanceMeters,
    double? RatingAverage,
    int? ReviewCount,
    decimal? StartingPrice,
    string Currency,
    List<string> PublicServices,
    VerificationStatus VerificationStatus,
    DateTime? NextAvailableAt,
    double? RankingScore = null
);

public record DiscoveryMarkerDto(
    Guid LocationId,
    Guid BusinessId,
    string BusinessSlug,
    string PublicName,
    double Latitude,
    double Longitude,
    string Category,
    double? RatingAverage,
    decimal? StartingPrice
);

public record DiscoveryResultResponse(
    List<ProviderLocationResult> Items,
    List<DiscoveryMarkerDto>? Markers,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    ResolvedLocationResult? ResolvedLocation,
    string SortModeUsed
);
