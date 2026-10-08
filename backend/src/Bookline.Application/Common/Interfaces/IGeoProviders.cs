namespace Bookline.Application.Common.Interfaces;

public record GeoCoordinates(double Latitude, double Longitude);

public record GeocodedLocation(
    string FormattedAddress,
    string City,
    string State,
    string Country,
    string PostalCode,
    double Latitude,
    double Longitude
);

public interface IGeocodingProvider
{
    Task<GeocodedLocation?> GeocodeAsync(string addressOrCity, CancellationToken cancellationToken = default);
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}

public interface IMapProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    string GetTileUrlTemplate();
    string? GetClientApiKey();
}
