using Bookline.Application.Discovery.Dtos;

namespace Bookline.Application.Common.Interfaces;

/// <summary>
/// Phase 4: Location Search and City Discovery Service.
/// Coordinates place search, selected location resolution, geographic viewport bounds,
/// and privacy-safe location handling.
/// </summary>
public interface ILocationDiscoveryService
{
    /// <summary>
    /// Searches for cities, neighborhood areas, and postal codes matching the query text.
    /// Preserves full geographic context including viewport bounds where available.
    /// </summary>
    Task<IReadOnlyList<PlaceSuggestionDto>> SearchPlacesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves location discovery parameters from user input (GPS, city, area, postcode, viewport).
    /// Applies deterministic conflict-resolution priority and ensures customer GPS coordinates
    /// remain transient and never leak into persistent URLs.
    /// </summary>
    Task<ResolvedLocationResult> ResolveLocationAsync(LocationResolutionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns featured discovery cities with their authoritative geographic bounding boxes and provider counts.
    /// </summary>
    Task<IReadOnlyList<CitySummaryDto>> GetFeaturedCitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific place suggestion by its canonical identifier.
    /// </summary>
    Task<PlaceSuggestionDto?> GetPlaceByIdAsync(string placeId, CancellationToken cancellationToken = default);
}
