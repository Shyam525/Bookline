using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Search;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class SearchIntentAndDiscoveryTests
{
    private readonly SearchIntentService _intentService = new();
    private readonly GeocodingProvider _geocodingProvider = new();

    [Fact]
    public void ParseIntent_SalonNearMe_ExtractsServiceAndNearMeIntent()
    {
        var result = _intentService.ParseIntent("salon near me");

        Assert.Equal("Salon", result.ExtractedService);
        Assert.Equal("Beauty & Wellness", result.ExtractedCategory);
        Assert.Equal("near_me", result.ExtractedIntent);
    }

    [Fact]
    public void ParseIntent_BestFacialInAhmedabad_ExtractsServiceLocationAndTopRatedIntent()
    {
        var result = _intentService.ParseIntent("best facial in Ahmedabad");

        Assert.Equal("Facial", result.ExtractedService);
        Assert.Equal("Ahmedabad", result.ExtractedLocation);
        Assert.Equal("top_rated", result.ExtractedIntent);
    }

    [Fact]
    public void ParseIntent_DentistNearSatellite_ExtractsServiceAreaAndCity()
    {
        var result = _intentService.ParseIntent("dentist near Satellite");

        Assert.Equal("Dental Consultation", result.ExtractedService);
        Assert.Equal("Satellite", result.ExtractedArea);
        Assert.Equal("Ahmedabad", result.ExtractedLocation);
    }

    [Fact]
    public void ParseIntent_MassageRajkot_ExtractsServiceAndCity()
    {
        var result = _intentService.ParseIntent("massage Rajkot");

        Assert.Equal("Massage", result.ExtractedService);
        Assert.Equal("Rajkot", result.ExtractedLocation);
    }

    [Fact]
    public async Task GeocodeAsync_PostalCode_ResolvesAreaAndCoordinates()
    {
        // 380015 corresponds to Satellite, Ahmedabad
        var result = await _geocodingProvider.GeocodeAsync("380015");

        Assert.NotNull(result);
        Assert.Equal("Ahmedabad", result.City);
        Assert.Equal("380015", result.PostalCode);
        Assert.True(result.Latitude > 0);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ValidCoords_ReturnsNearestCity()
    {
        // Bodakdev, Ahmedabad coordinates
        var city = await _geocodingProvider.ReverseGeocodeAsync(23.0396, 72.5074);

        Assert.Equal("Ahmedabad", city);
    }
}
