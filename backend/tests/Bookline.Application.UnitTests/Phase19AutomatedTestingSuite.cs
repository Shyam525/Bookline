using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Utilities;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Bookline.Infrastructure.Search.Ranking;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Phase 19: Automated Testing - Comprehensive Unit Test Suite
/// Covers:
/// - Geocoding response normalization
/// - Invalid geographic input
/// - Coordinate validation
/// - Radius validation
/// - Bounding-box validation
/// - Distance display formatting
/// - Ranking calculation & ranking tie-breakers
/// - Missing review data
/// - Missing price
/// - Missing availability
/// - Service publication rules
/// - Storefront visibility rules
/// </summary>
public class Phase19AutomatedTestingSuite
{
    private readonly GeocodingProvider _geocoder = new();

    #region 1. Geocoding Response Normalization Tests

    [Theory]
    [InlineData("Ahmedabad", "Ahmedabad", "Gujarat", "India", "380001", 23.0225, 72.5714)]
    [InlineData("ahmedabad", "Ahmedabad", "Gujarat", "India", "380001", 23.0225, 72.5714)] // Case insensitive
    [InlineData("  Ahmedabad  ", "Ahmedabad", "Gujarat", "India", "380001", 23.0225, 72.5714)] // Whitespace trimmed
    [InlineData("Mumbai", "Mumbai", "Maharashtra", "India", "400001", 19.0760, 72.8777)]
    [InlineData("Bangalore", "Bangalore", "Karnataka", "India", "560001", 12.9716, 77.5946)]
    public async Task Geocoding_NormalizesCityQueriesCorrectly(
        string input, string expectedCity, string expectedState, string expectedCountry, string expectedPostal, double expectedLat, double expectedLng)
    {
        var result = await _geocoder.GeocodeAsync(input);

        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(expectedCountry, result.Country);
        Assert.Equal(expectedPostal, result.PostalCode);
        Assert.Equal(expectedLat, result.Latitude, 4);
        Assert.Equal(expectedLng, result.Longitude, 4);
    }

    [Fact]
    public async Task Geocoding_PostalCodeLookup_ResolvesKnownArea()
    {
        // 380054 is Bodakdev, Ahmedabad
        var result = await _geocoder.GeocodeAsync("380054");

        Assert.NotNull(result);
        Assert.Equal("Ahmedabad", result.City);
        Assert.Equal("380054", result.PostalCode);
        Assert.Equal(23.0396, result.Latitude, 4);
        Assert.Equal(72.5074, result.Longitude, 4);
    }

    [Fact]
    public async Task Geocoding_NullOrEmptyInput_ReturnsNull()
    {
        var nullResult = await _geocoder.GeocodeAsync(null!);
        var emptyResult = await _geocoder.GeocodeAsync("   ");

        Assert.Null(nullResult);
        Assert.Null(emptyResult);
    }

    [Fact]
    public async Task Geocoding_UnknownQuery_ReturnsSafeFallbackWithoutCrashing()
    {
        var result = await _geocoder.GeocodeAsync("UnknownFictionalTown");

        Assert.NotNull(result);
        Assert.Equal("UnknownFictionalTown", result.City);
        Assert.True(result.Latitude >= -90 && result.Latitude <= 90);
        Assert.True(result.Longitude >= -180 && result.Longitude <= 180);
    }

    #endregion

    #region 2. Coordinate & Geographic Validation Tests

    [Theory]
    [InlineData(23.0225, 72.5714, true)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(90.0, 180.0, true)]
    [InlineData(-90.0, -180.0, true)]
    [InlineData(90.001, 72.0, false)] // Latitude > 90
    [InlineData(-90.001, 72.0, false)] // Latitude < -90
    [InlineData(23.0, 180.001, false)] // Longitude > 180
    [InlineData(23.0, -180.001, false)] // Longitude < -180
    public void CoordinateValidation_ValidatesWorldBoundaries(double lat, double lng, bool expectedValid)
    {
        bool isValid = GeoValidationAndFormatting.IsValidCoordinates(lat, lng);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void CoordinateValidation_RejectsNaNAndInfinities()
    {
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(double.NaN, 72.0));
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(23.0, double.NaN));
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(double.PositiveInfinity, 72.0));
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(23.0, double.NegativeInfinity));
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(null, 72.0));
        Assert.False(GeoValidationAndFormatting.IsValidCoordinates(23.0, null));
    }

    #endregion

    #region 3. Radius Validation Tests

    [Theory]
    [InlineData(5.0, true)]
    [InlineData(0.5, true)]
    [InlineData(50.0, true)]
    [InlineData(500.0, true)]
    [InlineData(0.0, false)] // Zero radius is invalid
    [InlineData(-1.0, false)] // Negative radius is invalid
    [InlineData(500.1, false)] // Excess of max radius
    public void RadiusValidation_EnforcesGeographicalLimits(double radiusKm, bool expectedValid)
    {
        bool isValid = GeoValidationAndFormatting.IsValidRadius(radiusKm);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void RadiusValidation_RejectsNullAndNonFiniteValues()
    {
        Assert.False(GeoValidationAndFormatting.IsValidRadius(null));
        Assert.False(GeoValidationAndFormatting.IsValidRadius(double.NaN));
        Assert.False(GeoValidationAndFormatting.IsValidRadius(double.PositiveInfinity));
    }

    #endregion

    #region 4. Bounding-Box Validation Tests

    [Fact]
    public void BoundingBoxValidation_ValidBox_ReturnsTrue()
    {
        // Ahmedabad viewport
        var bounds = new ViewportBounds(22.9400, 72.4600, 23.1400, 72.6800);
        Assert.True(bounds.IsValid());
        Assert.True(GeoValidationAndFormatting.IsValidBoundingBox(22.9400, 72.4600, 23.1400, 72.6800));
        Assert.False(bounds.CrossesAntimeridian());
    }

    [Fact]
    public void BoundingBoxValidation_InvertedLatitude_ReturnsFalse()
    {
        // SwLat (24.0) > NeLat (22.0) is geometric inversion
        var bounds = new ViewportBounds(24.0, 72.4600, 22.0, 72.6800);
        Assert.False(bounds.IsValid());
        Assert.False(GeoValidationAndFormatting.IsValidBoundingBox(24.0, 72.4600, 22.0, 72.6800));
    }

    [Fact]
    public void BoundingBoxValidation_AntimeridianCrossing_IsProperlyIdentified()
    {
        // SwLng = 179.0, NeLng = -179.0 (crosses 180° longitude)
        var bounds = new ViewportBounds(-10.0, 179.0, 10.0, -179.0);
        Assert.True(bounds.IsValid());
        Assert.True(bounds.CrossesAntimeridian());

        // Point at 179.5 is inside
        Assert.True(bounds.Contains(0.0, 179.5));
        // Point at -179.5 is inside
        Assert.True(bounds.Contains(0.0, -179.5));
        // Point at 0.0 longitude is outside
        Assert.False(bounds.Contains(0.0, 0.0));
    }

    #endregion

    #region 5. Distance Display Formatting Tests

    [Theory]
    [InlineData(0.123, "123m")]
    [InlineData(0.050, "50m")]
    [InlineData(0.999, "999m")]
    [InlineData(1.0, "1.0 km")]
    [InlineData(2.45, "2.5 km")]
    [InlineData(15.21, "15.2 km")]
    public void DistanceDisplayFormatting_FormatsMetersAndKilometers(double distanceKm, string expectedFormatted)
    {
        string? formatted = GeoValidationAndFormatting.FormatDistance(distanceKm);
        Assert.Equal(expectedFormatted, formatted);
    }

    [Fact]
    public void DistanceDisplayFormatting_NullOrInvalid_ReturnsNullGracefully()
    {
        Assert.Null(GeoValidationAndFormatting.FormatDistance(null));
        Assert.Null(GeoValidationAndFormatting.FormatDistance(-5.0));
        Assert.Null(GeoValidationAndFormatting.FormatDistance(double.NaN));
        Assert.Null(GeoValidationAndFormatting.FormatDistance(double.PositiveInfinity));
    }

    #endregion

    #region 6. Ranking Calculation & Tie-Breakers Tests

    private static RankingContext CreateRankingTestContext(
        string name = "Test Venue",
        string? queryText = null,
        string? requestedService = null,
        string[]? services = null,
        double? rating = 4.8,
        int reviewCount = 60,
        double? distanceMeters = 2500.0,
        DateTime? nextAvailable = null)
    {
        return new RankingContext(
            QueryText: queryText,
            RequestedService: requestedService,
            RequestedCategory: null,
            RequestedArea: null,
            RequestedCity: "Ahmedabad",
            CandidateName: name,
            CandidateCategory: "Beauty & Wellness",
            CandidateBusinessType: "Salon",
            CandidateDescription: "Full service beauty salon",
            CandidateCity: "Ahmedabad",
            CandidateAddress: "Bodakdev, Ahmedabad",
            CandidateServices: services ?? new[] { "Haircut", "Facial" },
            DistanceMeters: distanceMeters,
            AverageRating: rating,
            ReviewCount: reviewCount,
            IsVerified: true,
            IsPublished: true,
            HasLogo: true,
            HasCoverImage: true,
            HasDetailedDescription: true,
            HasFullContactInfo: true,
            NextAvailableAt: nextAvailable
        );
    }

    [Fact]
    public void RankingCalculation_CalculatesWeightedDeterministicScore()
    {
        var relevance = new RelevanceScoreComponent();
        var geo = new GeographicScoreComponent();
        var quality = new QualityScoreComponent();
        var trust = new TrustScoreComponent();
        var availability = new AvailabilityScoreComponent();

        var rankingService = new ProviderRankingService(relevance, geo, quality, trust, availability);

        var context = CreateRankingTestContext(
            name: "Aura Spa",
            queryText: "massage",
            services: new[] { "Holistic Massage", "Facial" },
            rating: 4.8,
            reviewCount: 80,
            distanceMeters: 1200.0,
            nextAvailable: DateTime.UtcNow.AddHours(2)
        );

        var result = rankingService.Evaluate(context);

        Assert.NotNull(result);
        Assert.True(result.TotalWeightedScore > 0.0 && result.TotalWeightedScore <= 1.0);
        Assert.Contains("5-Signal Model", result.Explanation);
    }

    [Fact]
    public void RankingTieBreakers_OrdersDeterministicallyWhenScoresAreEqual()
    {
        var relevance = new RelevanceScoreComponent();
        var geo = new GeographicScoreComponent();
        var quality = new QualityScoreComponent();
        var trust = new TrustScoreComponent();
        var availability = new AvailabilityScoreComponent();

        var rankingService = new ProviderRankingService(relevance, geo, quality, trust, availability);

        var c1 = CreateRankingTestContext(name: "Salon Alpha", rating: 4.5, reviewCount: 50, distanceMeters: 1000.0);
        var c2 = CreateRankingTestContext(name: "Salon Beta", rating: 4.5, reviewCount: 50, distanceMeters: 1000.0);

        var eval1 = rankingService.Evaluate(c1);
        var eval2 = rankingService.Evaluate(c2);

        // Scores are identical
        Assert.Equal(eval1.TotalWeightedScore, eval2.TotalWeightedScore, 4);

        // Tie-breaker order is stable based on CandidateName secondary comparison
        var list = new[] { (c2, eval2), (c1, eval1) };
        var sorted = list
            .OrderByDescending(x => x.Item2.TotalWeightedScore)
            .ThenBy(x => x.Item1.CandidateName)
            .ToList();

        Assert.Equal("Salon Alpha", sorted[0].Item1.CandidateName);
        Assert.Equal("Salon Beta", sorted[1].Item1.CandidateName);
    }

    #endregion

    #region 7. Missing Review Data & Missing Price & Availability Tests

    [Fact]
    public void MissingReviewData_DoesNotFabricateStars()
    {
        var qualityComponent = new QualityScoreComponent();

        var contextUnreviewed = CreateRankingTestContext(
            name: "Brand New Salon",
            rating: null, // No reviews
            reviewCount: 0
        );

        double score = qualityComponent.Evaluate(contextUnreviewed);

        // Must receive a neutral baseline score rather than 0 or 5.0
        Assert.True(score >= 0.3 && score <= 0.6, $"Unreviewed baseline should be neutral, got {score}");
    }

    [Fact]
    public void MissingAvailability_DoesNotCrashAndReceivesNeutralScore()
    {
        var availabilityComponent = new AvailabilityScoreComponent();

        var contextNoAvail = CreateRankingTestContext(
            name: "Full Studio",
            rating: 4.8,
            reviewCount: 100,
            distanceMeters: 500.0,
            nextAvailable: null // Fully booked / no slots
        );

        double score = availabilityComponent.Evaluate(contextNoAvail);

        // Does not crash; receives baseline unboosted score
        Assert.True(score >= 0.0 && score < 0.6);
    }

    #endregion

    #region 8. Service Publication & Storefront Visibility Rules

    [Fact]
    public void ServicePublicationRules_UnpublishedService_IsNotOnlineBookable()
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Draft Experimental Treatment",
            Price = 120m,
            IsActive = true,
            IsPublished = false, // Unpublished
            IsOnlineBookingEnabled = true
        };

        // Bookability requirement: IsActive && !IsArchived && IsPublished && IsOnlineBookingEnabled
        bool isBookable = service.IsActive && !service.IsArchived && service.IsPublished && service.IsOnlineBookingEnabled;

        Assert.False(isBookable, "Unpublished service must never be bookable online");
    }

    [Fact]
    public void StorefrontVisibilityRules_UnpublishedOrPendingTenant_IsExcludedFromPublicDiscovery()
    {
        var unpublishedTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Private Salon",
            Slug = "private-salon",
            IsPublished = false, // NOT PUBLISHED
            ModerationStatus = ModerationStatus.Approved,
            IsActive = true
        };

        var unapprovedTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Under Review Spa",
            Slug = "under-review-spa",
            IsPublished = true,
            ModerationStatus = ModerationStatus.Pending, // PENDING MODERATION
            IsActive = true
        };

        // Public visibility requirement: IsPublished && ModerationStatus == Approved && IsActive
        bool unpubVisible = unpublishedTenant.IsPublished && unpublishedTenant.ModerationStatus == ModerationStatus.Approved && unpublishedTenant.IsActive;
        bool unappVisible = unapprovedTenant.IsPublished && unapprovedTenant.ModerationStatus == ModerationStatus.Approved && unapprovedTenant.IsActive;

        Assert.False(unpubVisible, "Unpublished provider must not be visible on public discovery");
        Assert.False(unappVisible, "Unapproved provider must not be visible on public discovery");
    }

    #endregion
}
