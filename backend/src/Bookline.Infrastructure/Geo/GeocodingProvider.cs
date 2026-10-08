using Bookline.Application.Common.Interfaces;

namespace Bookline.Infrastructure.Geo;

public class GeocodingProvider : IGeocodingProvider
{
    private static readonly Dictionary<string, GeocodedLocation> KnownLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ahmedabad"] = new("Ahmedabad, Gujarat, India", "Ahmedabad", "Gujarat", "India", "380001", 23.0225, 72.5714),
        ["Satellite, Ahmedabad"] = new("Satellite, Ahmedabad, Gujarat, India", "Ahmedabad", "Gujarat", "India", "380015", 23.0305, 72.5178),
        ["Bodakdev, Ahmedabad"] = new("Bodakdev, Ahmedabad, Gujarat, India", "Ahmedabad", "Gujarat", "India", "380054", 23.0396, 72.5074),
        ["Navrangpura, Ahmedabad"] = new("Navrangpura, Ahmedabad, Gujarat, India", "Ahmedabad", "Gujarat", "India", "380009", 23.0373, 72.5539),
        ["Vastrapur, Ahmedabad"] = new("Vastrapur, Ahmedabad, Gujarat, India", "Ahmedabad", "Gujarat", "India", "380015", 23.0350, 72.5293),
        
        ["Rajkot"] = new("Rajkot, Gujarat, India", "Rajkot", "Gujarat", "India", "360001", 22.3039, 70.8022),
        ["Surat"] = new("Surat, Gujarat, India", "Surat", "Gujarat", "India", "395003", 21.1702, 72.8311),
        ["Vadodara"] = new("Vadodara, Gujarat, India", "Vadodara", "Gujarat", "India", "390001", 22.3072, 73.1812),

        ["Mumbai"] = new("Mumbai, Maharashtra, India", "Mumbai", "Maharashtra", "India", "400001", 19.0760, 72.8777),
        ["Bandra, Mumbai"] = new("Bandra West, Mumbai, Maharashtra, India", "Mumbai", "Maharashtra", "India", "400050", 19.0596, 72.8295),
        ["Andheri, Mumbai"] = new("Andheri West, Mumbai, Maharashtra, India", "Mumbai", "Maharashtra", "India", "400058", 19.1136, 72.8697),
        ["Juhu, Mumbai"] = new("Juhu, Mumbai, Maharashtra, India", "Mumbai", "Maharashtra", "India", "400049", 19.1075, 72.8263),

        ["Bangalore"] = new("Bengaluru, Karnataka, India", "Bangalore", "Karnataka", "India", "560001", 12.9716, 77.5946),
        ["Koramangala, Bangalore"] = new("Koramangala, Bengaluru, Karnataka, India", "Bangalore", "Karnataka", "India", "560034", 12.9352, 77.6245),
        ["Indiranagar, Bangalore"] = new("Indiranagar, Bengaluru, Karnataka, India", "Bangalore", "Karnataka", "India", "560038", 12.9784, 77.6408),
        ["Whitefield, Bangalore"] = new("Whitefield, Bengaluru, Karnataka, India", "Bangalore", "Karnataka", "India", "560066", 12.9698, 77.7500),

        ["Pune"] = new("Pune, Maharashtra, India", "Pune", "Maharashtra", "India", "411001", 18.5204, 73.8567),
        ["Delhi"] = new("New Delhi, Delhi, India", "Delhi", "Delhi", "India", "110001", 28.6139, 77.2090),
        ["London"] = new("London, Greater London, United Kingdom", "London", "England", "UK", "EC1A 1BB", 51.5074, -0.1278),
        ["New York"] = new("New York, NY, United States", "New York", "NY", "USA", "10001", 40.7128, -74.0060)
    };

    public Task<GeocodedLocation?> GeocodeAsync(string addressOrCity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(addressOrCity))
            return Task.FromResult<GeocodedLocation?>(null);

        var query = addressOrCity.Trim();

        // 1. Direct match
        if (KnownLocations.TryGetValue(query, out var directMatch))
            return Task.FromResult<GeocodedLocation?>(directMatch);

        // 2. Postal code match
        var postalMatch = KnownLocations.Values.FirstOrDefault(k =>
            k.PostalCode.Equals(query, StringComparison.OrdinalIgnoreCase));
        if (postalMatch != null)
            return Task.FromResult<GeocodedLocation?>(postalMatch);

        // 3. Contains match
        var partialMatch = KnownLocations.FirstOrDefault(k =>
            query.Contains(k.Key, StringComparison.OrdinalIgnoreCase) ||
            k.Key.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (partialMatch.Value != null)
            return Task.FromResult<GeocodedLocation?>(partialMatch.Value);

        // 3. Fallback default
        return Task.FromResult<GeocodedLocation?>(new GeocodedLocation(
            $"{query}, India",
            query,
            "State",
            "India",
            "380001",
            23.0225,
            72.5714
        ));
    }

    public Task<string?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        // Find nearest known city
        string? nearestCity = null;
        double minDistance = double.MaxValue;

        foreach (var loc in KnownLocations.Values)
        {
            var d = CalculateDistanceKm(latitude, longitude, loc.Latitude, loc.Longitude);
            if (d < minDistance)
            {
                minDistance = d;
                nearestCity = loc.City;
            }
        }

        return Task.FromResult(minDistance <= 100 ? nearestCity : "Ahmedabad");
    }

    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in kilometers
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(R * c, 2);
    }
}
