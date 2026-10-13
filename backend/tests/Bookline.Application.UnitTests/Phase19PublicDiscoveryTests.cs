using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Bookline.Infrastructure.Search.Ranking;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Phase 19: Public Discovery Integration Tests
/// Covers:
/// - Search by business name.
/// - Search by service.
/// - Search by category.
/// - Search by city.
/// - Search by area.
/// - Radius query.
/// - Viewport query.
/// - Nearest sort.
/// - Top-rated sort.
/// - Most-reviewed sort.
/// - Price filtering.
/// - Rating filtering.
/// - Pagination.
/// - No-results behavior.
/// - Private provider exclusion.
/// - Storefront tests.
/// - Service tests.
/// - Geocoding smoke tests.
/// </summary>
public class Phase19PublicDiscoveryTests
{
    private (BooklineDbContext Db, ProviderDiscoveryService DiscoveryService, PublicStorefrontService StorefrontService) CreateServices()
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: "Phase19DiscoveryDb_" + Guid.NewGuid())
            .Options;

        var db = new BooklineDbContext(options, tenantContext);

        var locationService = new LocationDiscoveryService();
        var rankingService = new ProviderRankingService(
            new RelevanceScoreComponent(),
            new GeographicScoreComponent(),
            new QualityScoreComponent(),
            new TrustScoreComponent(),
            new AvailabilityScoreComponent()
        );
        var intentService = new SearchIntentService();

        var discoveryService = new ProviderDiscoveryService(
            db,
            locationService,
            rankingService,
            intentService
        );

        var storefrontService = new PublicStorefrontService(db);

        return (db, discoveryService, storefrontService);
    }

    private async Task SeedDiscoveryVenuesAsync(BooklineDbContext db)
    {
        // 1. Aura Wellness - Bodakdev Ahmedabad (Rating 4.9, 120 reviews, Spa)
        var t1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Wellness & Spa",
            Slug = "aura-wellness",
            Category = "Beauty & Wellness",
            BusinessType = "Spa",
            City = "Ahmedabad",
            Address = "102 SG Highway, Bodakdev, Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            AverageRating = 4.9,
            ReviewCount = 120,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        // 2. Luxe Hair Salon - Satellite Ahmedabad (Rating 4.7, 85 reviews, Salon)
        var t2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Luxe Hair Salon",
            Slug = "luxe-salon",
            Category = "Hair & Beauty",
            BusinessType = "Salon",
            City = "Ahmedabad",
            Address = "Satellite Road, Ahmedabad",
            Latitude = 23.0280,
            Longitude = 72.5180,
            AverageRating = 4.7,
            ReviewCount = 85,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        // 3. Zenith Pilates - Bandra Mumbai (Rating 4.8, 200 reviews, Pilates)
        var t3 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Zenith Pilates Studio",
            Slug = "zenith-pilates",
            Category = "Fitness & Training",
            BusinessType = "Pilates",
            City = "Mumbai",
            Address = "Linking Road, Bandra West, Mumbai",
            Latitude = 19.0596,
            Longitude = 72.8295,
            AverageRating = 4.8,
            ReviewCount = 200,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        // 4. Private / Unpublished Venue (Must NEVER be returned publicly)
        var t4Hidden = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Secret Unlisted Parlor",
            Slug = "secret-parlor",
            Category = "Hair & Beauty",
            BusinessType = "Salon",
            City = "Ahmedabad",
            Address = "Bodakdev, Ahmedabad",
            Latitude = 23.0390,
            Longitude = 72.5070,
            AverageRating = 5.0,
            ReviewCount = 10,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Pending, // Pending moderation
            IsPublished = false, // NOT Published
            IsActive = true
        };

        db.Tenants.AddRange(t1, t2, t3, t4Hidden);

        // Add services
        var s1 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Name = "Holistic Aromatherapy Massage",
            Slug = "holistic-massage",
            Price = 120m,
            DurationMinutes = 60,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        var s2 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Name = "Deep Tissue Ritual",
            Slug = "deep-tissue",
            Price = 180m,
            DurationMinutes = 90,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        var s3 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t2.Id,
            Name = "Balayage & Styling",
            Slug = "balayage-styling",
            Price = 250m,
            DurationMinutes = 120,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        var s4Draft = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t2.Id,
            Name = "Experimental Hair Treatment",
            Slug = "experimental-hair",
            Price = 300m,
            DurationMinutes = 60,
            IsActive = true,
            IsPublished = false, // UNPUBLISHED SERVICE
            IsOnlineBookingEnabled = true
        };

        var s5 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t3.Id,
            Name = "Reformer Pilates Session",
            Slug = "reformer-pilates",
            Price = 60m,
            DurationMinutes = 50,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        db.Services.AddRange(s1, s2, s3, s4Draft, s5);

        // Add staff
        var staff1 = new Bookline.Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = t1.Id, Name = "Elena Vance", IsActive = true };
        var staff2 = new Bookline.Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = t2.Id, Name = "Marcus Brody", IsActive = true };
        var staff3 = new Bookline.Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = t3.Id, Name = "Sarah Jenkins", IsActive = true };

        db.Staff.AddRange(staff1, staff2, staff3);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchByBusinessName_FindsMatchingProvider()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(QueryText: "Aura");
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.NotEmpty(result.Items);
        Assert.Contains("Aura Wellness", result.Items[0].PublicName);
    }

    [Fact]
    public async Task SearchByService_FindsProviderOfferingService()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(QueryText: "Massage");
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.NotEmpty(result.Items);
        Assert.Equal("Aura Wellness & Spa", result.Items[0].PublicName);
    }

    [Fact]
    public async Task SearchByCategory_FiltersToRequestedCategory()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(CategorySlug: "fitness-training");
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.Single(result.Items);
        Assert.Equal("Zenith Pilates Studio", result.Items[0].PublicName);
    }

    [Fact]
    public async Task SearchByCity_FiltersExclusivelyToSelectedCity()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(
            SelectedPlace: new PlaceSuggestionDto(
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
                ViewportBounds: null,
                SuggestedRadiusMeters: 20000
            )
        );
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.Single(result.Items);
        Assert.Equal("Mumbai", result.Items[0].City);
    }

    [Fact]
    public async Task SearchByArea_FiltersToSpecifiedNeighborhood()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(
            SelectedPlace: new PlaceSuggestionDto(
                PlaceId: "area-satellite",
                DisplayName: "Satellite, Ahmedabad",
                City: "Ahmedabad",
                Area: "Satellite",
                PostalCode: "380015",
                State: "Gujarat",
                Country: "India",
                Latitude: 23.0305,
                Longitude: 72.5178,
                Type: LocationType.NeighborhoodArea,
                ViewportBounds: null,
                SuggestedRadiusMeters: 3500
            )
        );
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, i => i.PublicName == "Luxe Hair Salon");
    }

    [Fact]
    public async Task RadiusQuery_ScopesToGeographicRadius()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        // Near Bodakdev (23.0396, 72.5074) with small 1.5 km (1500m) radius
        var query = new DiscoveryQuery(
            Latitude: 23.0396,
            Longitude: 72.5074,
            RadiusMeters: 1500.0
        );

        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.Single(result.Items);
        Assert.Equal("Aura Wellness & Spa", result.Items[0].PublicName);
    }

    [Fact]
    public async Task ViewportQuery_ScopesToSpatialBoundingBox()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        // Viewport covering Mumbai only
        var query = new DiscoveryQuery(
            ViewportBounds: new ViewportBounds(19.0000, 72.8000, 19.1000, 72.9000)
        );

        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.Single(result.Items);
        Assert.Equal("Mumbai", result.Items[0].City);
    }

    [Fact]
    public async Task NearestSort_OrdersAscendingByDistance()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        // Bodakdev location: Aura (< 100m), Luxe (~1800m)
        var query = new DiscoveryQuery(
            Latitude: 23.0396,
            Longitude: 72.5074,
            SortMode: DiscoverySortMode.Nearest
        );

        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.True(result.Items.Count >= 2);
        Assert.Equal("Aura Wellness & Spa", result.Items[0].PublicName);
        Assert.Equal("Luxe Hair Salon", result.Items[1].PublicName);

        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.True(result.Items[i].DistanceMeters <= result.Items[i + 1].DistanceMeters);
        }
    }

    [Fact]
    public async Task TopRatedSort_OrdersDescendingByRating()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(SortMode: DiscoverySortMode.TopRated);
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.True(result.Items.Count >= 3);
        // Aura is 4.9, Zenith is 4.8, Luxe is 4.7
        Assert.Equal("Aura Wellness & Spa", result.Items[0].PublicName);
        Assert.Equal("Zenith Pilates Studio", result.Items[1].PublicName);
        Assert.Equal("Luxe Hair Salon", result.Items[2].PublicName);
    }

    [Fact]
    public async Task MostReviewedSort_OrdersDescendingByReviewCount()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(SortMode: DiscoverySortMode.MostReviewed);
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.True(result.Items.Count >= 3);
        // Zenith has 200 reviews, Aura has 120, Luxe has 85
        Assert.Equal("Zenith Pilates Studio", result.Items[0].PublicName);
        Assert.Equal("Aura Wellness & Spa", result.Items[1].PublicName);
        Assert.Equal("Luxe Hair Salon", result.Items[2].PublicName);
    }

    [Fact]
    public async Task RatingFilter_ExcludesProvidersBelowThreshold()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        // Min rating 4.8
        var query = new DiscoveryQuery(MinimumRating: 4.8);
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.All(result.Items, i => Assert.True(i.RatingAverage >= 4.8));
        Assert.DoesNotContain(result.Items, i => i.PublicName == "Luxe Hair Salon");
    }

    [Fact]
    public async Task Pagination_ReturnsCorrectPageAndPageCount()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var page1 = await discovery.DiscoverProvidersAsync(new DiscoveryQuery(Page: 1, PageSize: 1));
        var page2 = await discovery.DiscoverProvidersAsync(new DiscoveryQuery(Page: 2, PageSize: 1));

        Assert.Single(page1.Items);
        Assert.Single(page2.Items);
        Assert.NotEqual(page1.Items[0].BusinessId, page2.Items[0].BusinessId);
        Assert.True(page1.TotalCount >= 3);
        Assert.True(page1.TotalPages >= 3);
    }

    [Fact]
    public async Task NoResultsBehavior_ReturnsEmptyListWithoutException()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var query = new DiscoveryQuery(QueryText: "NonexistentUnderwaterWeldingService");
        var result = await discovery.DiscoverProvidersAsync(query);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task PrivateProviderExclusion_NeverReturnsUnpublishedOrUnmoderatedProvider()
    {
        var (db, discovery, _) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        // Query covering all providers
        var query = new DiscoveryQuery();
        var result = await discovery.DiscoverProvidersAsync(query);

        // "Secret Unlisted Parlor" must NEVER be returned
        Assert.DoesNotContain(result.Items, i => i.BusinessSlug == "secret-parlor");
        Assert.DoesNotContain(result.Items, i => i.PublicName.Contains("Secret"));
    }

    [Fact]
    public async Task StorefrontTests_PublishedProviderReturnsFullStorefrontProfile()
    {
        var (db, _, storefront) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var profile = await storefront.GetStorefrontBySlugAsync("aura-wellness");

        Assert.NotNull(profile);
        Assert.Equal("Aura Wellness & Spa", profile.Provider.Name);
        Assert.NotEmpty(profile.Services);
        // Only published services returned
        Assert.All(profile.Services, s => Assert.True(s.IsPublished));
    }

    [Fact]
    public async Task ServiceTests_ExcludesUnpublishedDraftServicesFromPublicView()
    {
        var (db, _, storefront) = CreateServices();
        await SeedDiscoveryVenuesAsync(db);

        var luxeStorefront = await storefront.GetStorefrontBySlugAsync("luxe-salon");

        Assert.NotNull(luxeStorefront);
        Assert.Single(luxeStorefront.Services);
        Assert.Equal("Balayage & Styling", luxeStorefront.Services[0].Name);
        // "Experimental Hair Treatment" was unpublished, so must not appear
        Assert.DoesNotContain(luxeStorefront.Services, s => s.Name.Contains("Experimental"));
    }

    [Fact]
    public async Task GeocodingSmokeTest_ValidatesProviderResolution()
    {
        var geocoder = new GeocodingProvider();

        // 1. Known city smoke test
        var ahmedabad = await geocoder.GeocodeAsync("Ahmedabad");
        Assert.NotNull(ahmedabad);
        Assert.Equal("Ahmedabad", ahmedabad.City);
        Assert.Equal(23.0225, ahmedabad.Latitude, 3);
        Assert.Equal(72.5714, ahmedabad.Longitude, 3);

        // 2. Reverse geocoding smoke test
        string? nearestCity = await geocoder.ReverseGeocodeAsync(23.0396, 72.5074);
        Assert.Equal("Ahmedabad", nearestCity);
    }
}
