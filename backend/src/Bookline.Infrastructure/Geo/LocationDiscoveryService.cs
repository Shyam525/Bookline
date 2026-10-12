using System.Text.RegularExpressions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;

namespace Bookline.Infrastructure.Geo;

public class LocationDiscoveryService : ILocationDiscoveryService
{
    private static readonly List<PlaceSuggestionDto> Directory = new()
    {
        // --- AHMEDABAD ---
        new PlaceSuggestionDto(
            PlaceId: "city-ahmedabad",
            DisplayName: "Ahmedabad, Gujarat",
            City: "Ahmedabad",
            Area: null,
            PostalCode: "380001",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0225,
            Longitude: 72.5714,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(22.9400, 72.4600, 23.1400, 72.6800),
            SuggestedRadiusMeters: 18000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-bodakdev-ahmedabad",
            DisplayName: "Bodakdev, Ahmedabad",
            City: "Ahmedabad",
            Area: "Bodakdev",
            PostalCode: "380054",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0396,
            Longitude: 72.5074,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(23.0250, 72.4950, 23.0550, 72.5200),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-satellite-ahmedabad",
            DisplayName: "Satellite, Ahmedabad",
            City: "Ahmedabad",
            Area: "Satellite",
            PostalCode: "380015",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0305,
            Longitude: 72.5178,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(23.0150, 72.5050, 23.0450, 72.5300),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-navrangpura-ahmedabad",
            DisplayName: "Navrangpura, Ahmedabad",
            City: "Ahmedabad",
            Area: "Navrangpura",
            PostalCode: "380009",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0373,
            Longitude: 72.5539,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(23.0250, 72.5400, 23.0500, 72.5680),
            SuggestedRadiusMeters: 3000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-vastrapur-ahmedabad",
            DisplayName: "Vastrapur, Ahmedabad",
            City: "Ahmedabad",
            Area: "Vastrapur",
            PostalCode: "380015",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0350,
            Longitude: 72.5293,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(23.0230, 72.5180, 23.0470, 72.5400),
            SuggestedRadiusMeters: 3000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-maninagar-ahmedabad",
            DisplayName: "Maninagar, Ahmedabad",
            City: "Ahmedabad",
            Area: "Maninagar",
            PostalCode: "380008",
            State: "Gujarat",
            Country: "India",
            Latitude: 22.9980,
            Longitude: 72.6050,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(22.9800, 72.5900, 23.0150, 72.6250),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-sindhubhavan-ahmedabad",
            DisplayName: "Sindhu Bhavan, Ahmedabad",
            City: "Ahmedabad",
            Area: "Sindhu Bhavan",
            PostalCode: "380054",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0450,
            Longitude: 72.5020,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(23.0300, 72.4880, 23.0600, 72.5150),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "postcode-380054",
            DisplayName: "380054 (Bodakdev / SG Highway)",
            City: "Ahmedabad",
            Area: "Bodakdev",
            PostalCode: "380054",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0396,
            Longitude: 72.5074,
            Type: LocationType.PostalCode,
            ViewportBounds: null,
            SuggestedRadiusMeters: 4000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "postcode-380015",
            DisplayName: "380015 (Satellite / Vastrapur)",
            City: "Ahmedabad",
            Area: "Satellite",
            PostalCode: "380015",
            State: "Gujarat",
            Country: "India",
            Latitude: 23.0305,
            Longitude: 72.5178,
            Type: LocationType.PostalCode,
            ViewportBounds: null,
            SuggestedRadiusMeters: 4000.0
        ),

        // --- MUMBAI ---
        new PlaceSuggestionDto(
            PlaceId: "city-mumbai",
            DisplayName: "Mumbai, Maharashtra",
            City: "Mumbai",
            Area: null,
            PostalCode: "400001",
            State: "Maharashtra",
            Country: "India",
            Latitude: 19.0760,
            Longitude: 72.8777,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(18.8900, 72.7700, 19.2800, 72.9900),
            SuggestedRadiusMeters: 25000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-bandra-mumbai",
            DisplayName: "Bandra West, Mumbai",
            City: "Mumbai",
            Area: "Bandra",
            PostalCode: "400050",
            State: "Maharashtra",
            Country: "India",
            Latitude: 19.0596,
            Longitude: 72.8295,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(19.0450, 72.8150, 19.0750, 72.8450),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-andheri-mumbai",
            DisplayName: "Andheri West, Mumbai",
            City: "Mumbai",
            Area: "Andheri",
            PostalCode: "400058",
            State: "Maharashtra",
            Country: "India",
            Latitude: 19.1136,
            Longitude: 72.8697,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(19.1000, 72.8450, 19.1300, 72.8850),
            SuggestedRadiusMeters: 4000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-juhu-mumbai",
            DisplayName: "Juhu, Mumbai",
            City: "Mumbai",
            Area: "Juhu",
            PostalCode: "400049",
            State: "Maharashtra",
            Country: "India",
            Latitude: 19.1075,
            Longitude: 72.8263,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(19.0950, 72.8150, 19.1200, 72.8380),
            SuggestedRadiusMeters: 3000.0
        ),

        // --- BANGALORE ---
        new PlaceSuggestionDto(
            PlaceId: "city-bangalore",
            DisplayName: "Bangalore, Karnataka",
            City: "Bangalore",
            Area: null,
            PostalCode: "560001",
            State: "Karnataka",
            Country: "India",
            Latitude: 12.9716,
            Longitude: 77.5946,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(12.8300, 77.4600, 13.1400, 77.7800),
            SuggestedRadiusMeters: 22000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-indiranagar-bangalore",
            DisplayName: "Indiranagar, Bangalore",
            City: "Bangalore",
            Area: "Indiranagar",
            PostalCode: "560038",
            State: "Karnataka",
            Country: "India",
            Latitude: 12.9784,
            Longitude: 77.6408,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(12.9650, 77.6250, 12.9900, 77.6550),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-koramangala-bangalore",
            DisplayName: "Koramangala, Bangalore",
            City: "Bangalore",
            Area: "Koramangala",
            PostalCode: "560034",
            State: "Karnataka",
            Country: "India",
            Latitude: 12.9352,
            Longitude: 77.6245,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(12.9200, 77.6100, 12.9500, 77.6400),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-whitefield-bangalore",
            DisplayName: "Whitefield, Bangalore",
            City: "Bangalore",
            Area: "Whitefield",
            PostalCode: "560066",
            State: "Karnataka",
            Country: "India",
            Latitude: 12.9698,
            Longitude: 77.7500,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(12.9450, 77.7200, 12.9950, 77.7800),
            SuggestedRadiusMeters: 5500.0
        ),

        // --- SURAT & RAJKOT ---
        new PlaceSuggestionDto(
            PlaceId: "city-surat",
            DisplayName: "Surat, Gujarat",
            City: "Surat",
            Area: null,
            PostalCode: "395003",
            State: "Gujarat",
            Country: "India",
            Latitude: 21.1702,
            Longitude: 72.8311,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(21.1200, 72.7500, 21.2500, 72.9000),
            SuggestedRadiusMeters: 14000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-vesu-surat",
            DisplayName: "Vesu, Surat",
            City: "Surat",
            Area: "Vesu",
            PostalCode: "395007",
            State: "Gujarat",
            Country: "India",
            Latitude: 21.1450,
            Longitude: 72.7800,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(21.1300, 72.7650, 21.1600, 72.7950),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-adajan-surat",
            DisplayName: "Adajan, Surat",
            City: "Surat",
            Area: "Adajan",
            PostalCode: "395009",
            State: "Gujarat",
            Country: "India",
            Latitude: 21.1950,
            Longitude: 72.7920,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(21.1800, 72.7750, 21.2100, 72.8100),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "city-rajkot",
            DisplayName: "Rajkot, Gujarat",
            City: "Rajkot",
            Area: null,
            PostalCode: "360001",
            State: "Gujarat",
            Country: "India",
            Latitude: 22.3039,
            Longitude: 70.8022,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(22.2500, 70.7500, 22.3500, 70.8500),
            SuggestedRadiusMeters: 12000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-kalawad-rajkot",
            DisplayName: "Kalawad Road, Rajkot",
            City: "Rajkot",
            Area: "Kalawad Road",
            PostalCode: "360005",
            State: "Gujarat",
            Country: "India",
            Latitude: 22.2850,
            Longitude: 70.7680,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(22.2700, 70.7500, 22.3000, 70.7850),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "city-pune",
            DisplayName: "Pune, Maharashtra",
            City: "Pune",
            Area: null,
            PostalCode: "411001",
            State: "Maharashtra",
            Country: "India",
            Latitude: 18.5204,
            Longitude: 73.8567,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(18.4200, 73.7500, 18.6200, 73.9600),
            SuggestedRadiusMeters: 18000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-koregaon-pune",
            DisplayName: "Koregaon Park, Pune",
            City: "Pune",
            Area: "Koregaon Park",
            PostalCode: "411001",
            State: "Maharashtra",
            Country: "India",
            Latitude: 18.5362,
            Longitude: 73.8940,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(18.5250, 73.8800, 18.5500, 73.9100),
            SuggestedRadiusMeters: 3500.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-baner-pune",
            DisplayName: "Baner, Pune",
            City: "Pune",
            Area: "Baner",
            PostalCode: "411045",
            State: "Maharashtra",
            Country: "India",
            Latitude: 18.5590,
            Longitude: 73.7868,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(18.5450, 73.7700, 18.5750, 73.8050),
            SuggestedRadiusMeters: 4000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "city-delhi",
            DisplayName: "Delhi / NCR",
            City: "Delhi",
            Area: null,
            PostalCode: "110001",
            State: "Delhi",
            Country: "India",
            Latitude: 28.6139,
            Longitude: 77.2090,
            Type: LocationType.City,
            ViewportBounds: new ViewportBounds(28.4000, 76.8500, 28.8800, 77.4000),
            SuggestedRadiusMeters: 30000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-connaught-delhi",
            DisplayName: "Connaught Place, Delhi",
            City: "Delhi",
            Area: "Connaught Place",
            PostalCode: "110001",
            State: "Delhi",
            Country: "India",
            Latitude: 28.6315,
            Longitude: 77.2167,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(28.6200, 77.2050, 28.6450, 77.2300),
            SuggestedRadiusMeters: 3000.0
        ),
        new PlaceSuggestionDto(
            PlaceId: "area-saket-delhi",
            DisplayName: "Saket, Delhi",
            City: "Delhi",
            Area: "Saket",
            PostalCode: "110017",
            State: "Delhi",
            Country: "India",
            Latitude: 28.5244,
            Longitude: 77.2100,
            Type: LocationType.NeighborhoodArea,
            ViewportBounds: new ViewportBounds(28.5100, 77.1950, 28.5400, 77.2250),
            SuggestedRadiusMeters: 3500.0
        )
    };

    public Task<IReadOnlyList<PlaceSuggestionDto>> SearchPlacesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Return top city choices by default
            var defaults = Directory.Where(d => d.Type == LocationType.City).Take(6).ToList();
            return Task.FromResult<IReadOnlyList<PlaceSuggestionDto>>(defaults);
        }

        var q = query.Trim().ToLowerInvariant();

        var matches = Directory.Where(d =>
            d.DisplayName.ToLowerInvariant().Contains(q) ||
            d.City.ToLowerInvariant().Contains(q) ||
            (d.Area != null && d.Area.ToLowerInvariant().Contains(q)) ||
            (d.PostalCode != null && d.PostalCode.Contains(q))
        )
        .OrderBy(d =>
        {
            // Exact prefix matches ranked higher
            if (d.DisplayName.ToLowerInvariant().StartsWith(q)) return 0;
            if (d.City.ToLowerInvariant().StartsWith(q)) return 1;
            if (d.Area != null && d.Area.ToLowerInvariant().StartsWith(q)) return 2;
            return 3;
        })
        .ThenBy(d => d.Type)
        .Take(10)
        .ToList();

        return Task.FromResult<IReadOnlyList<PlaceSuggestionDto>>(matches);
    }

    public Task<ResolvedLocationResult> ResolveLocationAsync(LocationResolutionRequest request, CancellationToken cancellationToken = default)
    {
        // =========================================================================
        // GEOGRAPHIC INPUT PRIORITY RULES
        // 1. Explicit Map Viewport (User dragging map or clicking "Search this area")
        // 2. Selected Place with defined Viewport Bounds (e.g. City or Area polygon)
        // 3. Selected Place point or Postal Code with suggested/explicit radius
        // 4. Current GPS Coordinates (transient GPS with explicit or default radius)
        // 5. City / Area Text Fallback
        // 6. Unrestricted
        // =========================================================================

        // 1. Map Viewport Bounds
        if (request.ViewportBounds != null && request.ViewportBounds.IsValid())
        {
            var center = request.ViewportBounds.GetCenter();
            var refPoint = (request.Latitude.HasValue && request.Longitude.HasValue)
                ? new GeoPoint(request.Latitude.Value, request.Longitude.Value)
                : new GeoPoint(center.CenterLatitude, center.CenterLongitude);

            return Task.FromResult(new ResolvedLocationResult(
                ResolutionType: LocationResolutionType.MapViewport,
                SelectedPlace: null,
                ReferenceCoordinates: refPoint,
                EffectiveViewportBounds: request.ViewportBounds,
                EffectiveRadiusMeters: null,
                IsPreciseGpsTransient: request.IsCurrentGpsLocation,
                PrivacySafeShareableSlug: $"viewport={request.ViewportBounds.SouthWestLatitude:F4},{request.ViewportBounds.SouthWestLongitude:F4},{request.ViewportBounds.NorthEastLatitude:F4},{request.ViewportBounds.NorthEastLongitude:F4}",
                FilterCity: null,
                FilterArea: null,
                FilterPostalCode: null,
                Explanation: "Spatial filtering via active map viewport bounding box."
            ));
        }

        // 2. Canonical Place by PlaceId
        PlaceSuggestionDto? place = null;
        if (!string.IsNullOrWhiteSpace(request.PlaceId))
        {
            place = Directory.FirstOrDefault(d => d.PlaceId.Equals(request.PlaceId, StringComparison.OrdinalIgnoreCase));
        }

        // If no place id, attempt to resolve by city/area
        if (place == null && (!string.IsNullOrWhiteSpace(request.City) || !string.IsNullOrWhiteSpace(request.Area)))
        {
            if (!string.IsNullOrWhiteSpace(request.Area))
            {
                place = Directory.FirstOrDefault(d =>
                    d.Area != null && d.Area.Equals(request.Area, StringComparison.OrdinalIgnoreCase));
            }

            if (place == null && !string.IsNullOrWhiteSpace(request.City))
            {
                place = Directory.FirstOrDefault(d =>
                    d.Type == LocationType.City && d.City.Equals(request.City, StringComparison.OrdinalIgnoreCase));
            }
        }

        // If matched a place with defined Viewport Bounds, PREFER USING BOUNDS over a tiny center radius!
        if (place != null)
        {
            var refCoords = new GeoPoint(place.Latitude, place.Longitude);
            // If caller provided GPS coordinates, keep them as distance reference, but bounded by place
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                refCoords = new GeoPoint(request.Latitude.Value, request.Longitude.Value);
            }

            if (place.ViewportBounds != null && place.ViewportBounds.IsValid())
            {
                var resType = place.Type == LocationType.City
                    ? LocationResolutionType.SelectedCityWithBounds
                    : LocationResolutionType.SelectedAreaWithBounds;

                string shareSlug = place.Area != null
                    ? $"city={Uri.EscapeDataString(place.City.ToLowerInvariant())}&area={Uri.EscapeDataString(place.Area.ToLowerInvariant())}"
                    : $"city={Uri.EscapeDataString(place.City.ToLowerInvariant())}";

                return Task.FromResult(new ResolvedLocationResult(
                    ResolutionType: resType,
                    SelectedPlace: place,
                    ReferenceCoordinates: refCoords,
                    EffectiveViewportBounds: place.ViewportBounds,
                    EffectiveRadiusMeters: null, // Geographic viewport is preferred for city/area discovery
                    IsPreciseGpsTransient: request.IsCurrentGpsLocation,
                    PrivacySafeShareableSlug: shareSlug,
                    FilterCity: place.City,
                    FilterArea: place.Area,
                    FilterPostalCode: place.PostalCode,
                    Explanation: $"Geographic viewport bounds for {place.DisplayName} preferred over center radius."
                ));
            }

            // Place has no viewport bounds (e.g. postal code or point location) -> use explicit or suggested radius
            double radiusMeters = request.RadiusMeters ?? place.SuggestedRadiusMeters;
            string pointSlug = place.PostalCode != null
                ? $"postcode={place.PostalCode}"
                : $"place={place.PlaceId}";

            return Task.FromResult(new ResolvedLocationResult(
                ResolutionType: place.Type == LocationType.PostalCode ? LocationResolutionType.PostalCodeRadius : LocationResolutionType.SelectedAreaWithBounds,
                SelectedPlace: place,
                ReferenceCoordinates: refCoords,
                EffectiveViewportBounds: null,
                EffectiveRadiusMeters: radiusMeters,
                IsPreciseGpsTransient: request.IsCurrentGpsLocation,
                PrivacySafeShareableSlug: pointSlug,
                FilterCity: place.City,
                FilterArea: place.Area,
                FilterPostalCode: place.PostalCode,
                Explanation: $"Point location with configured search radius {radiusMeters:N0} meters."
            ));
        }

        // 3. Postal Code input
        if (!string.IsNullOrWhiteSpace(request.PostalCode))
        {
            var postcodeMatch = Directory.FirstOrDefault(d =>
                d.PostalCode != null && d.PostalCode.Equals(request.PostalCode.Trim(), StringComparison.OrdinalIgnoreCase));

            if (postcodeMatch != null)
            {
                double r = request.RadiusMeters ?? postcodeMatch.SuggestedRadiusMeters;
                return Task.FromResult(new ResolvedLocationResult(
                    ResolutionType: LocationResolutionType.PostalCodeRadius,
                    SelectedPlace: postcodeMatch,
                    ReferenceCoordinates: new GeoPoint(postcodeMatch.Latitude, postcodeMatch.Longitude),
                    EffectiveViewportBounds: null,
                    EffectiveRadiusMeters: r,
                    IsPreciseGpsTransient: false,
                    PrivacySafeShareableSlug: $"postcode={request.PostalCode.Trim()}",
                    FilterCity: postcodeMatch.City,
                    FilterArea: postcodeMatch.Area,
                    FilterPostalCode: request.PostalCode.Trim(),
                    Explanation: $"Postal code {request.PostalCode} resolved with {r:N0}m radius."
                ));
            }
        }

        // 4. Current GPS Coordinates (Customer Location Privacy: Transient, not persisted, shareable slug is sanitized)
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            double r = request.RadiusMeters ?? 10000.0; // Default 10 km proximity radius

            // Reverse approximate city
            var nearestCity = Directory
                .Where(d => d.Type == LocationType.City)
                .OrderBy(d => GeocodingProvider.CalculateDistanceKm(request.Latitude.Value, request.Longitude.Value, d.Latitude, d.Longitude))
                .FirstOrDefault();

            string shareSlug = nearestCity != null
                ? $"city={Uri.EscapeDataString(nearestCity.City.ToLowerInvariant())}&nearme=true"
                : "nearme=true";

            return Task.FromResult(new ResolvedLocationResult(
                ResolutionType: LocationResolutionType.CurrentGpsTransient,
                SelectedPlace: null,
                ReferenceCoordinates: new GeoPoint(request.Latitude.Value, request.Longitude.Value),
                EffectiveViewportBounds: null,
                EffectiveRadiusMeters: r,
                IsPreciseGpsTransient: true, // Privacy flag: do not persist to user profile by default!
                PrivacySafeShareableSlug: shareSlug,
                FilterCity: nearestCity?.City,
                FilterArea: null,
                FilterPostalCode: null,
                Explanation: $"Transient current GPS coordinates evaluated with {r:N0}m radius. Precise coordinates omitted from shareable URLs."
            ));
        }

        // 5. City / Area Text Fallback
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            string shareSlug = $"city={Uri.EscapeDataString(request.City.Trim().ToLowerInvariant())}";
            if (!string.IsNullOrWhiteSpace(request.Area))
            {
                shareSlug += $"&area={Uri.EscapeDataString(request.Area.Trim().ToLowerInvariant())}";
            }

            return Task.FromResult(new ResolvedLocationResult(
                ResolutionType: LocationResolutionType.CityAreaTextFallback,
                SelectedPlace: null,
                ReferenceCoordinates: null,
                EffectiveViewportBounds: null,
                EffectiveRadiusMeters: null,
                IsPreciseGpsTransient: false,
                PrivacySafeShareableSlug: shareSlug,
                FilterCity: request.City.Trim(),
                FilterArea: request.Area?.Trim(),
                FilterPostalCode: null,
                Explanation: $"Textual city/area search for '{request.City}' without geographic coordinates."
            ));
        }

        // 6. Unrestricted Discovery
        return Task.FromResult(new ResolvedLocationResult(
            ResolutionType: LocationResolutionType.Unrestricted,
            SelectedPlace: null,
            ReferenceCoordinates: null,
            EffectiveViewportBounds: null,
            EffectiveRadiusMeters: null,
            IsPreciseGpsTransient: false,
            PrivacySafeShareableSlug: string.Empty,
            FilterCity: null,
            FilterArea: null,
            FilterPostalCode: null,
            Explanation: "Unrestricted global marketplace discovery."
        ));
    }

    public Task<IReadOnlyList<CitySummaryDto>> GetFeaturedCitiesAsync(CancellationToken cancellationToken = default)
    {
        var cities = Directory
            .Where(d => d.Type == LocationType.City && d.ViewportBounds != null)
            .Select(c =>
            {
                var popularAreas = Directory
                    .Where(d => d.City.Equals(c.City, StringComparison.OrdinalIgnoreCase) && d.Area != null)
                    .Select(d => d.Area!)
                    .Distinct()
                    .Take(5)
                    .ToList();

                int providerCount = c.City switch
                {
                    "Ahmedabad" => 16,
                    "Mumbai" => 24,
                    "Bangalore" => 20,
                    "Surat" => 9,
                    "Rajkot" => 8,
                    "Pune" => 12,
                    _ => 10
                };

                return new CitySummaryDto(
                    Name: c.City,
                    State: c.State ?? string.Empty,
                    Country: c.Country,
                    Latitude: c.Latitude,
                    Longitude: c.Longitude,
                    ViewportBounds: c.ViewportBounds!,
                    ProviderCount: providerCount,
                    PopularAreas: popularAreas
                );
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<CitySummaryDto>>(cities);
    }

    public Task<PlaceSuggestionDto?> GetPlaceByIdAsync(string placeId, CancellationToken cancellationToken = default)
    {
        var match = Directory.FirstOrDefault(d => d.PlaceId.Equals(placeId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }
}
