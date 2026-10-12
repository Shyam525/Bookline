using Bookline.Application.Discovery.Dtos;
using Bookline.Infrastructure.Geo;
using Xunit;

namespace Bookline.Application.UnitTests;

public class LocationDiscoveryServiceTests
{
    private readonly LocationDiscoveryService _service = new();

    [Fact]
    public async Task SearchPlaces_WithCityQuery_ReturnsMatchingCityWithViewportBounds()
    {
        // Act
        var places = await _service.SearchPlacesAsync("Ahmedabad");

        // Assert
        Assert.NotEmpty(places);
        var city = places.FirstOrDefault(p => p.City == "Ahmedabad" && p.Type == LocationType.City);
        Assert.NotNull(city);
        Assert.NotNull(city.ViewportBounds);
        Assert.True(city.ViewportBounds.IsValid());
        Assert.True(city.SuggestedRadiusMeters > 10000, "City suggested radius should be macro scale (>10km)");
    }

    [Fact]
    public async Task SearchPlaces_WithNeighborhoodQuery_ReturnsAreaWithParentCityContext()
    {
        // Act
        var places = await _service.SearchPlacesAsync("Bodakdev");

        // Assert
        Assert.NotEmpty(places);
        var area = places.FirstOrDefault(p => p.Area == "Bodakdev");
        Assert.NotNull(area);
        Assert.Equal("Ahmedabad", area.City);
        Assert.Equal(LocationType.NeighborhoodArea, area.Type);
        Assert.NotNull(area.ViewportBounds);
        Assert.Equal("380054", area.PostalCode);
    }

    [Fact]
    public async Task ResolveLocation_SelectedCity_PrefersViewportBoundsOverTinyRadius()
    {
        // Arrange
        var request = new LocationResolutionRequest(City: "Ahmedabad");

        // Act
        var resolved = await _service.ResolveLocationAsync(request);

        // Assert
        Assert.Equal(LocationResolutionType.SelectedCityWithBounds, resolved.ResolutionType);
        Assert.NotNull(resolved.EffectiveViewportBounds);
        Assert.True(resolved.EffectiveViewportBounds.IsValid());
        // Acceptance criterion: Never infer that every city is adequately represented by a tiny radius around center
        Assert.Null(resolved.EffectiveRadiusMeters);
        Assert.Equal("Ahmedabad", resolved.FilterCity);
        Assert.False(resolved.IsPreciseGpsTransient);
        Assert.Equal("city=ahmedabad", resolved.PrivacySafeShareableSlug);
    }

    [Fact]
    public async Task ResolveLocation_SelectedArea_PreservesGeographicContextAndBounds()
    {
        // Arrange
        var request = new LocationResolutionRequest(City: "Ahmedabad", Area: "Satellite");

        // Act
        var resolved = await _service.ResolveLocationAsync(request);

        // Assert
        Assert.Equal(LocationResolutionType.SelectedAreaWithBounds, resolved.ResolutionType);
        Assert.NotNull(resolved.EffectiveViewportBounds);
        Assert.Equal("Ahmedabad", resolved.FilterCity);
        Assert.Equal("Satellite", resolved.FilterArea);
        Assert.Equal("city=ahmedabad&area=satellite", resolved.PrivacySafeShareableSlug);
    }

    [Fact]
    public async Task ResolveLocation_CurrentGpsCoordinates_PreservesPrivacyAndTransientState()
    {
        // Arrange
        var request = new LocationResolutionRequest(
            Latitude: 23.0396472,
            Longitude: 72.5074128,
            IsCurrentGpsLocation: true
        );

        // Act
        var resolved = await _service.ResolveLocationAsync(request);

        // Assert
        Assert.Equal(LocationResolutionType.CurrentGpsTransient, resolved.ResolutionType);
        Assert.True(resolved.IsPreciseGpsTransient, "GPS coordinates must be flagged transient for privacy");
        Assert.NotNull(resolved.ReferenceCoordinates);
        Assert.Equal(23.0396472, resolved.ReferenceCoordinates.Latitude);
        Assert.Equal(72.5074128, resolved.ReferenceCoordinates.Longitude);
        // Acceptance criterion: Do not put exact current coordinates into shareable URLs
        Assert.DoesNotContain("23.0396472", resolved.PrivacySafeShareableSlug);
        Assert.DoesNotContain("72.5074128", resolved.PrivacySafeShareableSlug);
        Assert.Contains("nearme=true", resolved.PrivacySafeShareableSlug);
    }

    [Fact]
    public async Task ResolveLocation_MapViewportTakesPriorityOverSelectedPlace()
    {
        // Arrange: Map viewport covering West Ahmedabad passed together with a SelectedPlace ID for Mumbai
        var mapViewport = new ViewportBounds(23.0100, 72.4900, 23.0600, 72.5300);
        var request = new LocationResolutionRequest(
            PlaceId: "city-mumbai",
            ViewportBounds: mapViewport
        );

        // Act
        var resolved = await _service.ResolveLocationAsync(request);

        // Assert: Map viewport must take authoritative priority
        Assert.Equal(LocationResolutionType.MapViewport, resolved.ResolutionType);
        Assert.Equal(mapViewport, resolved.EffectiveViewportBounds);
    }

    [Fact]
    public async Task ResolveLocation_PostalCode_ResolvesExplicitRadius()
    {
        // Arrange
        var request = new LocationResolutionRequest(PostalCode: "380054");

        // Act
        var resolved = await _service.ResolveLocationAsync(request);

        // Assert
        Assert.Equal(LocationResolutionType.PostalCodeRadius, resolved.ResolutionType);
        Assert.NotNull(resolved.EffectiveRadiusMeters);
        Assert.True(resolved.EffectiveRadiusMeters > 0);
        Assert.Equal("380054", resolved.FilterPostalCode);
        Assert.Equal("postcode=380054", resolved.PrivacySafeShareableSlug);
    }

    [Fact]
    public void ViewportBounds_AntimeridianCrossing_EvaluatesContainmentCorrectly()
    {
        // Viewport crossing longitude 180 (e.g. from 170.0 to -170.0)
        var bounds = new ViewportBounds(
            SouthWestLatitude: -20.0,
            SouthWestLongitude: 170.0,
            NorthEastLatitude: 20.0,
            NorthEastLongitude: -170.0
        );

        Assert.True(bounds.IsValid());
        Assert.True(bounds.CrossesAntimeridian());

        // Point at 175.0 longitude is inside
        Assert.True(bounds.Contains(0.0, 175.0));
        // Point at -175.0 longitude is inside
        Assert.True(bounds.Contains(0.0, -175.0));
        // Point at 0.0 longitude (Prime Meridian) is outside
        Assert.False(bounds.Contains(0.0, 0.0));
    }

    [Fact]
    public void ViewportBounds_InvalidCoordinates_Rejected()
    {
        var invalidLat = new ViewportBounds(95.0, 72.0, 96.0, 73.0);
        Assert.False(invalidLat.IsValid());

        var invertedLat = new ViewportBounds(30.0, 72.0, 20.0, 73.0);
        Assert.False(invertedLat.IsValid());

        var invalidLng = new ViewportBounds(20.0, -190.0, 30.0, 73.0);
        Assert.False(invalidLng.IsValid());
    }

    [Fact]
    public async Task GetFeaturedCities_ReturnsMajorMetrosWithValidBoundingBoxes()
    {
        // Act
        var cities = await _service.GetFeaturedCitiesAsync();

        // Assert
        Assert.NotEmpty(cities);
        Assert.Contains(cities, c => c.Name == "Ahmedabad");
        Assert.Contains(cities, c => c.Name == "Mumbai");
        Assert.Contains(cities, c => c.Name == "Bangalore");

        Assert.All(cities, c =>
        {
            Assert.True(c.ViewportBounds.IsValid());
            Assert.True(c.ProviderCount > 0);
            Assert.NotEmpty(c.PopularAreas);
        });
    }
}
