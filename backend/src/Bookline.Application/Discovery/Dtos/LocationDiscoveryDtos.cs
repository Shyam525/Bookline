namespace Bookline.Application.Discovery.Dtos;

public enum LocationType
{
    City = 1,
    NeighborhoodArea = 2,
    PostalCode = 3,
    Coordinates = 4,
    Viewport = 5
}

public enum LocationResolutionType
{
    MapViewport = 1,
    SelectedCityWithBounds = 2,
    SelectedAreaWithBounds = 3,
    PostalCodeRadius = 4,
    CurrentGpsTransient = 5,
    CityAreaTextFallback = 6,
    Unrestricted = 7
}

/// <summary>
/// Geographic bounding box defined by southwest and northeast corners.
/// Supports both standard bounding boxes and antimeridian-crossing bounds.
/// </summary>
public record ViewportBounds(
    double SouthWestLatitude,
    double SouthWestLongitude,
    double NorthEastLatitude,
    double NorthEastLongitude
)
{
    public bool IsValid()
    {
        if (SouthWestLatitude < -90.0 || SouthWestLatitude > 90.0) return false;
        if (NorthEastLatitude < -90.0 || NorthEastLatitude > 90.0) return false;
        if (SouthWestLongitude < -180.0 || SouthWestLongitude > 180.0) return false;
        if (NorthEastLongitude < -180.0 || NorthEastLongitude > 180.0) return false;
        if (SouthWestLatitude > NorthEastLatitude) return false;
        return true;
    }

    /// <summary>
    /// Checks if the bounding box crosses the 180th meridian (antimeridian).
    /// When SwLng > NeLng, the box crosses from positive to negative longitudes.
    /// </summary>
    public bool CrossesAntimeridian() => SouthWestLongitude > NorthEastLongitude;

    public bool Contains(double latitude, double longitude)
    {
        if (latitude < SouthWestLatitude || latitude > NorthEastLatitude)
            return false;

        if (CrossesAntimeridian())
        {
            // Crosses antimeridian: point is inside if >= SwLng OR <= NeLng
            return longitude >= SouthWestLongitude || longitude <= NorthEastLongitude;
        }

        return longitude >= SouthWestLongitude && longitude <= NorthEastLongitude;
    }

    public (double CenterLatitude, double CenterLongitude) GetCenter()
    {
        double centerLat = (SouthWestLatitude + NorthEastLatitude) / 2.0;
        double centerLng;

        if (CrossesAntimeridian())
        {
            double span = (180.0 - SouthWestLongitude) + (NorthEastLongitude - (-180.0));
            centerLng = SouthWestLongitude + (span / 2.0);
            if (centerLng > 180.0) centerLng -= 360.0;
        }
        else
        {
            centerLng = (SouthWestLongitude + NorthEastLongitude) / 2.0;
        }

        return (centerLat, centerLng);
    }
}

public record PlaceSuggestionDto(
    string PlaceId,
    string DisplayName,
    string City,
    string? Area,
    string? PostalCode,
    string? State,
    string Country,
    double Latitude,
    double Longitude,
    LocationType Type,
    ViewportBounds? ViewportBounds,
    double SuggestedRadiusMeters
);

public record CitySummaryDto(
    string Name,
    string State,
    string Country,
    double Latitude,
    double Longitude,
    ViewportBounds ViewportBounds,
    int ProviderCount,
    IReadOnlyList<string> PopularAreas
);

public record LocationResolutionRequest(
    string? PlaceId = null,
    string? City = null,
    string? Area = null,
    string? PostalCode = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusMeters = null,
    ViewportBounds? ViewportBounds = null,
    bool IsCurrentGpsLocation = false,
    string? RecentLocationId = null
);

public record ResolvedLocationResult(
    LocationResolutionType ResolutionType,
    PlaceSuggestionDto? SelectedPlace,
    GeoPoint? ReferenceCoordinates,
    ViewportBounds? EffectiveViewportBounds,
    double? EffectiveRadiusMeters,
    bool IsPreciseGpsTransient,
    string PrivacySafeShareableSlug,
    string? FilterCity,
    string? FilterArea,
    string? FilterPostalCode,
    string Explanation
);

public record GeoPoint(double Latitude, double Longitude);
