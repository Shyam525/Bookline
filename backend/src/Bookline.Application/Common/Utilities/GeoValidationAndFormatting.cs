namespace Bookline.Application.Common.Utilities;

/// <summary>
/// Domain utility for geographic coordinates validation, bounding-box checks, and distance formatting.
/// </summary>
public static class GeoValidationAndFormatting
{
    /// <summary>
    /// Validates latitude and longitude within world boundaries [-90..90, -180..180] and rejects NaN / Infinity.
    /// </summary>
    public static bool IsValidCoordinates(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
            return false;

        double lat = latitude.Value;
        double lng = longitude.Value;

        if (double.IsNaN(lat) || double.IsInfinity(lat) || double.IsNaN(lng) || double.IsInfinity(lng))
            return false;

        return lat >= -90.0 && lat <= 90.0 && lng >= -180.0 && lng <= 180.0;
    }

    /// <summary>
    /// Validates a search radius in kilometers. Must be positive, finite, and within reasonable geographical limits.
    /// </summary>
    public static bool IsValidRadius(double? radiusKm, double minKm = 0.1, double maxKm = 500.0)
    {
        if (!radiusKm.HasValue)
            return false;

        double r = radiusKm.Value;

        if (double.IsNaN(r) || double.IsInfinity(r))
            return false;

        return r >= minKm && r <= maxKm;
    }

    /// <summary>
    /// Validates a geographic bounding box. SouthWestLatitude must not exceed NorthEastLatitude.
    /// </summary>
    public static bool IsValidBoundingBox(double swLat, double swLng, double neLat, double neLng)
    {
        if (!IsValidCoordinates(swLat, swLng) || !IsValidCoordinates(neLat, neLng))
            return false;

        if (swLat > neLat)
            return false; // Inverted latitudes are geometrically invalid

        return true;
    }

    /// <summary>
    /// Formats geographic distance for customer display:
    /// - &lt; 1 km: formatted in meters (e.g., "450m")
    /// - &gt;= 1 km: formatted in kilometers with one decimal place (e.g., "2.4 km")
    /// - Null or invalid values return null.
    /// </summary>
    public static string? FormatDistance(double? distanceKm)
    {
        if (!distanceKm.HasValue || double.IsNaN(distanceKm.Value) || double.IsInfinity(distanceKm.Value) || distanceKm.Value < 0)
            return null;

        double km = distanceKm.Value;

        if (km < 1.0)
        {
            int meters = (int)Math.Round(km * 1000.0);
            return $"{meters}m";
        }

        return $"{km:F1} km";
    }
}
