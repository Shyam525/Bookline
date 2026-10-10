using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Specification Section 131: GEO TESTS
/// - Current location granted.
/// - Current location denied.
/// - Manual city.
/// - Manual area.
/// - Radius.
/// - Bounding box.
/// - Nearest.
/// - Map movement.
/// - Search this area.
/// </summary>
public class GeoSearchSpecificationTests
{
    private BooklineDbContext CreateContext()
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    private async Task SeedMarketplaceVenuesAsync(BooklineDbContext db)
    {
        // 1. Ahmedabad - Bodakdev
        var v1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Wellness Bodakdev",
            Slug = "aura-bodakdev",
            Category = "Beauty & Wellness",
            BusinessType = "Spa",
            City = "Ahmedabad",
            Address = "Bodakdev, SG Highway, Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            AverageRating = 4.9,
            ReviewCount = 120,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        // 2. Ahmedabad - Satellite (~2 km from Bodakdev)
        var v2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Luxe Salon Satellite",
            Slug = "luxe-satellite",
            Category = "Beauty & Wellness",
            BusinessType = "Salon",
            City = "Ahmedabad",
            Address = "Satellite Road, Ahmedabad",
            Latitude = 23.0280,
            Longitude = 72.5180,
            AverageRating = 4.7,
            ReviewCount = 85,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        // 3. Ahmedabad - Maninagar (~12 km east from Bodakdev)
        var v3 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Eastern Health Studio",
            Slug = "eastern-health",
            Category = "Healthcare & Clinics",
            BusinessType = "Clinic",
            City = "Ahmedabad",
            Address = "Maninagar, Ahmedabad",
            Latitude = 22.9980,
            Longitude = 72.6050,
            AverageRating = 4.6,
            ReviewCount = 45,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        // 4. Mumbai - Bandra West
        var v4 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Zenith Pilates Bandra",
            Slug = "zenith-bandra",
            Category = "Fitness & Training",
            BusinessType = "Pilates",
            City = "Mumbai",
            Address = "Linking Road, Bandra West, Mumbai",
            Latitude = 19.0596,
            Longitude = 72.8295,
            AverageRating = 4.8,
            ReviewCount = 95,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        // 5. Bangalore - Indiranagar
        var v5 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Apex Studio Indiranagar",
            Slug = "apex-indiranagar",
            Category = "Photography & Media",
            BusinessType = "Studio",
            City = "Bangalore",
            Address = "100 Feet Road, Indiranagar, Bangalore",
            Latitude = 12.9784,
            Longitude = 73.6441,
            AverageRating = 4.9,
            ReviewCount = 110,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        db.Tenants.AddRange(v1, v2, v3, v4, v5);

        // Add sample services
        db.Services.AddRange(
            new Service { Id = Guid.NewGuid(), TenantId = v1.Id, Name = "Spa Therapy", Price = 75m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = v2.Id, Name = "Hair Styling", Price = 45m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = v3.Id, Name = "Health Consultation", Price = 60m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = v4.Id, Name = "Reformer Pilates", Price = 50m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = v5.Id, Name = "Portrait Session", Price = 90m, IsActive = true }
        );

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Section131_01_CurrentLocationGranted_CalculatesAccurateDistanceKm()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // User is at Bodakdev intersection (23.0390, 72.5070)
        var query = new ProviderSearchQuery(
            Latitude: 23.0390,
            Longitude: 72.5070
        );

        // Act
        var result = await searchService.SearchProvidersAsync(query);

        // Assert
        Assert.NotEmpty(result.Items);
        var bodakdevVenue = result.Items.FirstOrDefault(i => i.Slug == "aura-bodakdev");
        Assert.NotNull(bodakdevVenue);
        Assert.NotNull(bodakdevVenue.DistanceKm);
        Assert.True(bodakdevVenue.DistanceKm < 0.5, $"Distance should be < 0.5 km, was {bodakdevVenue.DistanceKm}");
    }

    [Fact]
    public async Task Section131_02_CurrentLocationDenied_GracefullyReturnsResultsWithoutDistance()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // User denied GPS permissions (Latitude and Longitude are null)
        var query = new ProviderSearchQuery(
            Latitude: null,
            Longitude: null
        );

        // Act
        var result = await searchService.SearchProvidersAsync(query);

        // Assert: Returns results without crashing, DistanceKm is null
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Null(item.DistanceKm));
    }

    [Fact]
    public async Task Section131_03_ManualCity_FiltersExclusivelyToSelectedCity()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Act: Manually select "Mumbai"
        var mumbaiResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery(City: "Mumbai"));

        // Assert: Only Mumbai venues returned
        Assert.Single(mumbaiResult.Items);
        Assert.Equal("Zenith Pilates Bandra", mumbaiResult.Items[0].Name);
        Assert.Equal("Mumbai", mumbaiResult.Items[0].City);

        // Act: Manually select "Bangalore"
        var blrResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery(City: "Bangalore"));

        // Assert: Only Bangalore venues returned
        Assert.Single(blrResult.Items);
        Assert.Equal("Apex Studio Indiranagar", blrResult.Items[0].Name);
        Assert.Equal("Bangalore", blrResult.Items[0].City);
    }

    [Fact]
    public async Task Section131_04_ManualArea_FiltersToSpecifiedNeighborhood()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Act: Query for Bodakdev neighborhood
        var result = await searchService.SearchProvidersAsync(new ProviderSearchQuery(
            Query: "salon in Bodakdev"
        ));

        // Assert: Matches Bodakdev venue, excludes Satellite and Maninagar
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Contains("Bodakdev", item.Address, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Section131_05_Radius_IncludesWithinRadiusAndExcludesOutside()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // User at Bodakdev, 5 km radius
        var query5Km = new ProviderSearchQuery(
            Latitude: 23.0396,
            Longitude: 72.5074,
            RadiusKm: 5.0
        );

        // Act
        var result = await searchService.SearchProvidersAsync(query5Km);

        // Assert: Bodakdev (<0.1km) and Satellite (~2km) included; Maninagar (~12km) and Mumbai excluded
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, i => i.Slug == "aura-bodakdev");
        Assert.Contains(result.Items, i => i.Slug == "luxe-satellite");
        Assert.DoesNotContain(result.Items, i => i.Slug == "eastern-health");
    }

    [Fact]
    public async Task Section131_06_BoundingBox_FiltersToSpatialViewportBounds()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Viewport covering West Ahmedabad (Bodakdev & Satellite only)
        // SwLat: 23.0100, SwLng: 72.4900, NeLat: 23.0600, NeLng: 72.5300
        var bboxQuery = new ProviderSearchQuery(
            SwLat: 23.0100,
            SwLng: 72.4900,
            NeLat: 23.0600,
            NeLng: 72.5300
        );

        // Act
        var result = await searchService.SearchProvidersAsync(bboxQuery);

        // Assert: Bodakdev and Satellite fall in bounding box; Maninagar (72.6050) falls outside
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.Latitude >= 23.0100 && item.Latitude <= 23.0600);
            Assert.True(item.Longitude >= 72.4900 && item.Longitude <= 72.5300);
        });
    }

    [Fact]
    public async Task Section131_07_Nearest_OrdersAscendingByDistance()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // User at Bodakdev (23.0396, 72.5074)
        var nearestQuery = new ProviderSearchQuery(
            City: "Ahmedabad",
            Latitude: 23.0396,
            Longitude: 72.5074,
            Sort: "Nearest"
        );

        // Act
        var result = await searchService.SearchProvidersAsync(nearestQuery);

        // Assert: Ordered by DistanceKm ascending: Bodakdev (0km) -> Satellite (~1.7km) -> Maninagar (~11km)
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("aura-bodakdev", result.Items[0].Slug);
        Assert.Equal("luxe-satellite", result.Items[1].Slug);
        Assert.Equal("eastern-health", result.Items[2].Slug);

        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.True(result.Items[i].DistanceKm <= result.Items[i + 1].DistanceKm);
        }
    }

    [Fact]
    public async Task Section131_08_MapMovement_RecalculatesResultsForNewViewport()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Initial viewport: Ahmedabad West
        var viewportA = new ProviderSearchQuery(
            SwLat: 23.0100, SwLng: 72.4900,
            NeLat: 23.0600, NeLng: 72.5300
        );

        // User moves/pans map to Mumbai viewport
        var viewportB = new ProviderSearchQuery(
            SwLat: 19.0000, SwLng: 72.8000,
            NeLat: 19.1000, NeLng: 72.9000
        );

        // Act
        var resultA = await searchService.SearchProvidersAsync(viewportA);
        var resultB = await searchService.SearchProvidersAsync(viewportB);

        // Assert
        Assert.Equal(2, resultA.Items.Count);
        Assert.All(resultA.Items, item => Assert.Equal("Ahmedabad", item.City));

        Assert.Single(resultB.Items);
        Assert.Equal("Mumbai", resultB.Items[0].City);
        Assert.Equal("zenith-bandra", resultB.Items[0].Slug);
    }

    [Fact]
    public async Task Section131_09_SearchThisArea_AppliesUpdatedViewportCenterAndBounds()
    {
        // Arrange
        using var db = CreateContext();
        await SeedMarketplaceVenuesAsync(db);
        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // User clicks "Search this area" over Maninagar / East Ahmedabad
        // Map Center: 22.9980, 72.6050
        // Bounds: 22.9700, 72.5800 to 23.0200, 72.6300
        var searchThisAreaQuery = new ProviderSearchQuery(
            Latitude: 22.9980,
            Longitude: 72.6050,
            SwLat: 22.9700,
            SwLng: 72.5800,
            NeLat: 23.0200,
            NeLng: 72.6300,
            Sort: "Nearest"
        );

        // Act
        var result = await searchService.SearchProvidersAsync(searchThisAreaQuery);

        // Assert: Scoped exclusively to East Ahmedabad; distance calculated from new center
        Assert.Single(result.Items);
        var venue = result.Items[0];
        Assert.Equal("eastern-health", venue.Slug);
        Assert.NotNull(venue.DistanceKm);
        Assert.True(venue.DistanceKm < 0.1, $"Distance from new center should be < 0.1 km, was {venue.DistanceKm}");
    }
}
