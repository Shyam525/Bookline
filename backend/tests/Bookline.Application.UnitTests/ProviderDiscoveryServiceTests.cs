using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Bookline.Infrastructure.Search.Ranking;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ProviderDiscoveryServiceTests
{
    private (BooklineDbContext Db, ProviderDiscoveryService DiscoveryService) CreateService()
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

        return (db, discoveryService);
    }

    private async Task SeedDataAsync(BooklineDbContext db)
    {
        // Business 1: Aura Wellness - Multi-Branch (Bodakdev & Vastrapur branches)
        var auraTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Wellness & Spa",
            Slug = "aura-wellness",
            Category = "Beauty & Wellness",
            BusinessType = "Spa",
            City = "Ahmedabad",
            Address = "Headquarters, Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            AverageRating = 4.9,
            ReviewCount = 120,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        var auraBranch1 = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = auraTenant.Id,
            Name = "Bodakdev Flagship",
            Address = "102 SG Highway, Bodakdev",
            City = "Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            IsActive = true,
            IsArchived = false
        };

        var auraBranch2 = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = auraTenant.Id,
            Name = "Vastrapur Lounge",
            Address = "Vastrapur Lake Road, Vastrapur",
            City = "Ahmedabad",
            Latitude = 23.0350,
            Longitude = 72.5293,
            IsActive = true,
            IsArchived = false
        };

        // Business 2: Glow Lounge Mumbai (Single Location via Tenant)
        var glowTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Lounge",
            Slug = "glow-lounge",
            Category = "Beauty & Wellness",
            BusinessType = "Salon",
            City = "Mumbai",
            Address = "Bandra West, Mumbai",
            Latitude = 19.0596,
            Longitude = 72.8295,
            AverageRating = 4.8,
            ReviewCount = 95,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        // Business 3: Unpublished / Unmoderated Venue (Must be excluded by server-side public visibility)
        var hiddenTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Secret Unapproved Parlor",
            Slug = "secret-parlor",
            Category = "Beauty & Wellness",
            BusinessType = "Salon",
            City = "Ahmedabad",
            Address = "Satellite, Ahmedabad",
            Latitude = 23.0305,
            Longitude = 72.5178,
            AverageRating = 5.0,
            ReviewCount = 10,
            ModerationStatus = ModerationStatus.Pending, // NOT APPROVED
            IsPublished = false, // NOT PUBLISHED
            IsActive = true
        };

        // Business 4: Venue with zero reviews
        var newTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Brand New Studio",
            Slug = "new-studio",
            Category = "Fitness & Training",
            BusinessType = "Pilates",
            City = "Ahmedabad",
            Address = "Navrangpura, Ahmedabad",
            Latitude = 23.0373,
            Longitude = 72.5539,
            AverageRating = 0.0,
            ReviewCount = 0,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            IsPublished = true,
            IsActive = true
        };

        db.Tenants.AddRange(auraTenant, glowTenant, hiddenTenant, newTenant);
        db.Locations.AddRange(auraBranch1, auraBranch2);

        // Services
        db.Services.AddRange(
            new Service { Id = Guid.NewGuid(), TenantId = auraTenant.Id, Name = "Aromatherapy Massage", Price = 80m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = auraTenant.Id, Name = "Deep Tissue Ritual", Price = 120m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = glowTenant.Id, Name = "Balayage & Styling", Price = 150m, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = newTenant.Id, Name = "Reformer Pilates Class", Price = 45m, IsActive = true }
        );

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Discover_MultiBranchBusiness_ProducesSeparateLocationResults()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // Act: Search Ahmedabad
        var query = new DiscoveryQuery(
            SelectedPlace: new PlaceSuggestionDto(
                PlaceId: "city-ahmedabad",
                DisplayName: "Ahmedabad",
                City: "Ahmedabad",
                Area: null,
                PostalCode: null,
                State: "Gujarat",
                Country: "India",
                Latitude: 23.0225,
                Longitude: 72.5714,
                Type: LocationType.City,
                ViewportBounds: new ViewportBounds(22.9400, 72.4600, 23.1400, 72.6800),
                SuggestedRadiusMeters: 18000.0
            )
        );

        var result = await service.DiscoverProvidersAsync(query);

        // Assert: Aura Wellness should produce 2 separate location results (Bodakdev & Vastrapur)
        var auraResults = result.Items.Where(i => i.BusinessSlug == "aura-wellness").ToList();
        Assert.Equal(2, auraResults.Count);

        var locationIds = auraResults.Select(r => r.LocationId).Distinct().ToList();
        Assert.Equal(2, locationIds.Count);

        Assert.Contains(auraResults, r => r.PublicName.Contains("Bodakdev"));
        Assert.Contains(auraResults, r => r.PublicName.Contains("Vastrapur"));
    }

    [Fact]
    public async Task Discover_ServerSidePublicVisibility_ExcludesUnapprovedOrUnpublishedProviders()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // Act
        var result = await service.DiscoverProvidersAsync(new DiscoveryQuery(QueryText: "Secret"));

        // Assert: Hidden venue must never be returned
        Assert.Empty(result.Items);
        Assert.DoesNotContain(result.Items, i => i.BusinessSlug == "secret-parlor");
    }

    [Fact]
    public async Task Discover_ComputesAccurateDistance_WhenReferenceCoordinatesProvided()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // User is at Bodakdev (23.0396, 72.5074)
        var query = new DiscoveryQuery(
            Latitude: 23.0396,
            Longitude: 72.5074
        );

        // Act
        var result = await service.DiscoverProvidersAsync(query);

        // Assert: Distance in meters should be calculated accurately
        var bodakdevBranch = result.Items.FirstOrDefault(i => i.PublicName.Contains("Bodakdev"));
        Assert.NotNull(bodakdevBranch);
        Assert.NotNull(bodakdevBranch.DistanceMeters);
        Assert.True(bodakdevBranch.DistanceMeters < 50.0, $"Bodakdev distance should be < 50m, was {bodakdevBranch.DistanceMeters}");

        var vastrapurBranch = result.Items.FirstOrDefault(i => i.PublicName.Contains("Vastrapur"));
        Assert.NotNull(vastrapurBranch);
        Assert.NotNull(vastrapurBranch.DistanceMeters);
        Assert.True(vastrapurBranch.DistanceMeters > 2000.0, "Vastrapur distance should be > 2km");
    }

    [Fact]
    public async Task Discover_NeverFabricatesDistance_WhenReferenceCoordinatesAbsent()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // User didn't provide GPS and queried by text "Ahmedabad"
        var query = new DiscoveryQuery(QueryText: "Aura");

        // Act
        var result = await service.DiscoverProvidersAsync(query);

        // Assert: DistanceMeters must be null for all items
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Null(i.DistanceMeters));
    }

    [Fact]
    public async Task Discover_ZeroReviews_DoesNotFabricateRatings()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        var query = new DiscoveryQuery(QueryText: "Brand New Studio");

        // Act
        var result = await service.DiscoverProvidersAsync(query);

        // Assert: New venue with 0 reviews should have null rating average
        Assert.Single(result.Items);
        var venue = result.Items[0];
        Assert.Null(venue.RatingAverage);
        Assert.Null(venue.ReviewCount);
    }

    [Fact]
    public async Task Discover_NeverFabricatesAvailability_NextAvailableAtIsNullWhenNotComputed()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // Act
        var result = await service.DiscoverProvidersAsync(new DiscoveryQuery());

        // Assert: NextAvailableAt is null because real availability engine was not queried
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Null(i.NextAvailableAt));
    }

    [Fact]
    public async Task Discover_InvalidInputs_ThrowsValidationException()
    {
        var (db, service) = CreateService();

        // 1. Latitude > 90
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(Latitude: 95.0, Longitude: 72.0)));

        // 2. Longitude < -180
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(Latitude: 23.0, Longitude: -195.0)));

        // 3. Radius negative
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(RadiusMeters: -500.0)));

        // 4. Radius exceeds 100,000m
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(RadiusMeters: 150_000.0)));

        // 5. Inverted bounding box (SwLat > NeLat)
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(
                ViewportBounds: new ViewportBounds(30.0, 72.0, 20.0, 73.0)
            )));

        // 6. PageSize > 50
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DiscoverProvidersAsync(new DiscoveryQuery(PageSize: 100)));
    }

    [Fact]
    public async Task Discover_ServerSidePagination_EnforcesPageAndSizeDeterministically()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // Act: Page 1, Size 2
        var p1 = await service.DiscoverProvidersAsync(new DiscoveryQuery(Page: 1, PageSize: 2));
        var p2 = await service.DiscoverProvidersAsync(new DiscoveryQuery(Page: 2, PageSize: 2));

        // Assert
        Assert.Equal(2, p1.Items.Count);
        Assert.Equal(1, p1.Page);
        Assert.Equal(2, p1.PageSize);
        Assert.True(p1.TotalCount >= 3);
        Assert.True(p1.TotalPages >= 2);

        // Items on page 2 must be different from page 1
        var p1Ids = p1.Items.Select(i => i.LocationId).ToHashSet();
        Assert.All(p2.Items, i => Assert.DoesNotContain(i.LocationId, p1Ids));
    }

    [Fact]
    public async Task Discover_IncludeMarkers_ReturnsLightweightMarkerDataset()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // Act
        var result = await service.DiscoverProvidersAsync(new DiscoveryQuery(IncludeMarkers: true));

        // Assert
        Assert.NotNull(result.Markers);
        Assert.NotEmpty(result.Markers);
        Assert.All(result.Markers, m =>
        {
            Assert.NotEqual(Guid.Empty, m.LocationId);
            Assert.NotEqual(Guid.Empty, m.BusinessId);
            Assert.NotEmpty(m.PublicName);
            Assert.True(m.Latitude != 0);
            Assert.True(m.Longitude != 0);
        });
    }

    [Fact]
    public async Task Discover_SortModes_ExecuteWithDeterministicTieBreakers()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedDataAsync(db);

        // 1. Nearest sort
        var nearest = await service.DiscoverProvidersAsync(new DiscoveryQuery(
            Latitude: 23.0396, Longitude: 72.5074, SortMode: DiscoverySortMode.Nearest
        ));
        Assert.NotEmpty(nearest.Items);
        for (int i = 0; i < nearest.Items.Count - 1; i++)
        {
            if (nearest.Items[i].DistanceMeters.HasValue && nearest.Items[i + 1].DistanceMeters.HasValue)
            {
                Assert.True(nearest.Items[i].DistanceMeters <= nearest.Items[i + 1].DistanceMeters);
            }
        }

        // 2. Lowest Price sort
        var lowestPrice = await service.DiscoverProvidersAsync(new DiscoveryQuery(SortMode: DiscoverySortMode.LowestPrice));
        Assert.NotEmpty(lowestPrice.Items);
        var pricedItems = lowestPrice.Items.Where(i => i.StartingPrice.HasValue).ToList();
        for (int i = 0; i < pricedItems.Count - 1; i++)
        {
            Assert.True(pricedItems[i].StartingPrice <= pricedItems[i + 1].StartingPrice);
        }

        // 3. Top Rated sort
        var topRated = await service.DiscoverProvidersAsync(new DiscoveryQuery(SortMode: DiscoverySortMode.TopRated));
        Assert.NotEmpty(topRated.Items);
        Assert.StartsWith("Aura Wellness & Spa", topRated.Items[0].PublicName);
    }
}
