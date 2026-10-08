using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class MarketplaceInfrastructureTests
{
    private BooklineDbContext CreateInMemoryContext()
    {
        var tenantContext = new Bookline.Application.Common.Models.TenantContext();
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task GeocodingProvider_ShouldResolveKnownCitiesAccurately()
    {
        // Arrange
        var provider = new GeocodingProvider();

        // Act
        var ahmedabad = await provider.GeocodeAsync("Ahmedabad");
        var mumbai = await provider.GeocodeAsync("Mumbai");
        var reverse = await provider.ReverseGeocodeAsync(23.0225, 72.5714);

        // Assert
        Assert.NotNull(ahmedabad);
        Assert.Equal(23.0225, ahmedabad.Latitude);
        Assert.Equal(72.5714, ahmedabad.Longitude);

        Assert.NotNull(mumbai);
        Assert.Equal(19.0760, mumbai.Latitude);

        Assert.Equal("Ahmedabad", reverse);
    }

    [Fact]
    public void Haversine_Distance_Calculation_ShouldBeAccurate()
    {
        // Ahmedabad to Rajkot
        double dist = GeocodingProvider.CalculateDistanceKm(23.0225, 72.5714, 22.3039, 70.8022);

        // Distance is ~200-220 km
        Assert.True(dist > 190 && dist < 230, $"Distance was {dist} km");
    }

    [Fact]
    public async Task ProviderSearchService_ShouldFilterAndRankProvidersCorrectly()
    {
        // Arrange
        var db = CreateInMemoryContext();

        var t1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Luxury Wellness",
            Slug = "aura-wellness",
            Category = "Beauty & Wellness",
            BusinessType = "Spa",
            City = "Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            AverageRating = 4.9,
            ReviewCount = 120,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        var t2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Zenith Fitness Gym",
            Slug = "zenith-fitness",
            Category = "Fitness & Training",
            BusinessType = "Pilates",
            City = "Mumbai",
            Latitude = 19.0760,
            Longitude = 72.8777,
            AverageRating = 4.5,
            ReviewCount = 30,
            VerificationStatus = VerificationStatus.Unverified,
            IsPublished = true,
            IsActive = true
        };

        db.Tenants.AddRange(t1, t2);

        var s1 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Name = "Aroma Facial",
            Price = 60.00m,
            IsActive = true
        };
        db.Services.Add(s1);

        await db.SaveChangesAsync();

        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Act 1: Search by category Beauty
        var catResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery
        {
            Category = "Beauty"
        });

        // Assert 1
        Assert.Single(catResult.Items);
        Assert.Equal("Aura Luxury Wellness", catResult.Items[0].Name);
        Assert.Equal(60.00m, catResult.Items[0].StartingPrice);

        // Act 2: Search with coordinates near Ahmedabad
        var geoResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery
        {
            Latitude = 23.0390,
            Longitude = 72.5070,
            RadiusKm = 10
        });

        // Assert 2
        Assert.Single(geoResult.Items);
        Assert.Equal("Aura Luxury Wellness", geoResult.Items[0].Name);
        Assert.NotNull(geoResult.Items[0].DistanceKm);
        Assert.True(geoResult.Items[0].DistanceKm < 2.0);
    }
}
