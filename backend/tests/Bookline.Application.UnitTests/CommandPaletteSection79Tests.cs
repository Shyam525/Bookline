using Bookline.Api.Controllers;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
using Bookline.Application.Customers.Handlers;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class CommandPaletteSection79Tests
{
    private BooklineDbContext CreateDbContext(string dbName, Guid? activeTenantId = null)
    {
        var tenantContext = new TenantContext();
        if (activeTenantId.HasValue)
        {
            tenantContext.SetTenantId(activeTenantId.Value);
        }

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    private static DiscoveryController CreateDiscoveryController(BooklineDbContext db)
    {
        // Dummy/mock implementations for required interfaces not used in service search
        var searchService = new FakeProviderSearchService();
        var discoveryEngine = new FakeProviderDiscoveryService();
        var locationService = new FakeLocationDiscoveryService();
        var geocoding = new FakeGeocodingProvider();

        return new DiscoveryController(searchService, discoveryEngine, locationService, geocoding, db);
    }

    [Fact]
    public async Task SearchPublicServices_ReturnsOnlyActiveOnlineBookableServicesOfPublishedTenants()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantPublished = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Wellness Spa",
            Slug = "aura-wellness",
            Category = "Beauty & Wellness",
            City = "Ahmedabad",
            IsActive = true,
            IsPublished = true,
            Currency = "INR"
        };

        var tenantUnpublished = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Draft Studio",
            Slug = "draft-studio",
            Category = "Hair & Styling",
            City = "Ahmedabad",
            IsActive = true,
            IsPublished = false, // Not published
            Currency = "INR"
        };

        var tenantInactive = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Suspended Lounge",
            Slug = "suspended-lounge",
            Category = "Beauty & Wellness",
            City = "Ahmedabad",
            IsActive = false, // Suspended / Inactive
            IsPublished = true,
            Currency = "INR"
        };

        var categoryId = Guid.NewGuid();

        // Services for Published Tenant
        var service1 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantPublished.Id,
            CategoryId = categoryId,
            Name = "Aromatherapy Massage",
            DurationMinutes = 60,
            Price = 1800m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        var serviceDisabledBooking = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantPublished.Id,
            CategoryId = categoryId,
            Name = "Private Consultation Only",
            DurationMinutes = 30,
            Price = 500m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = false // Should not be bookable publicly
        };

        var serviceArchived = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantPublished.Id,
            CategoryId = categoryId,
            Name = "Discontinued Treatment",
            DurationMinutes = 45,
            Price = 900m,
            IsActive = true,
            IsArchived = true, // Archived
            IsOnlineBookingEnabled = true
        };

        var serviceInactive = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantPublished.Id,
            CategoryId = categoryId,
            Name = "Inactive Facial",
            DurationMinutes = 45,
            Price = 1200m,
            IsActive = false, // Inactive
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        // Service for Unpublished Tenant
        var serviceUnpublished = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantUnpublished.Id,
            CategoryId = categoryId,
            Name = "Secret Cut",
            DurationMinutes = 45,
            Price = 700m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        // Service for Inactive Tenant
        var serviceInactiveTenant = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantInactive.Id,
            CategoryId = categoryId,
            Name = "Suspended Haircut",
            DurationMinutes = 30,
            Price = 600m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        using (var db = CreateDbContext(dbName))
        {
            db.Tenants.AddRange(tenantPublished, tenantUnpublished, tenantInactive);
            db.Services.AddRange(
                service1,
                serviceDisabledBooking,
                serviceArchived,
                serviceInactive,
                serviceUnpublished,
                serviceInactiveTenant
            );
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName))
        {
            var controller = CreateDiscoveryController(db);
            var actionResult = await controller.SearchPublicServices(q: null, limit: 10);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);

            var items = (System.Collections.IEnumerable)okResult.Value!;
            var list = items.Cast<object>().ToList();

            // Only service1 from the active published tenant must be returned
            Assert.Single(list);

            var item = list[0];
            var propName = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString();
            Assert.Equal("Aromatherapy Massage", propName);

            var providerName = item.GetType().GetProperty("ProviderName")?.GetValue(item)?.ToString();
            Assert.Equal("Aura Wellness Spa", providerName);

            var providerSlug = item.GetType().GetProperty("ProviderSlug")?.GetValue(item)?.ToString();
            Assert.Equal("aura-wellness", providerSlug);
        }
    }

    [Fact]
    public async Task SearchPublicServices_FiltersBySearchQueryAcrossNameAndCity()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantMumbai = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Studio",
            Slug = "glow-studio",
            Category = "Hair & Styling",
            City = "Mumbai",
            IsActive = true,
            IsPublished = true,
            Currency = "INR"
        };

        var tenantBangalore = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Zenith Dental",
            Slug = "zenith-dental",
            Category = "Healthcare",
            City = "Bangalore",
            IsActive = true,
            IsPublished = true,
            Currency = "INR"
        };

        var catId = Guid.NewGuid();
        var serviceMumbai = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantMumbai.Id,
            CategoryId = catId,
            Name = "Balayage Gloss",
            Description = "Full head luxury balayage with color gloss",
            DurationMinutes = 90,
            Price = 3500m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        var serviceBangalore = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBangalore.Id,
            CategoryId = catId,
            Name = "Teeth Whitening",
            Description = "Laser cosmetic whitening session",
            DurationMinutes = 45,
            Price = 4500m,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        using (var db = CreateDbContext(dbName))
        {
            db.Tenants.AddRange(tenantMumbai, tenantBangalore);
            db.Services.AddRange(serviceMumbai, serviceBangalore);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName))
        {
            var controller = CreateDiscoveryController(db);

            // 1. Search by service name
            var resName = await controller.SearchPublicServices(q: "Balayage");
            var okName = Assert.IsType<OkObjectResult>(resName);
            var listName = ((System.Collections.IEnumerable)okName.Value!).Cast<object>().ToList();
            Assert.Single(listName);

            // 2. Search by city
            var resCity = await controller.SearchPublicServices(q: "Bangalore");
            var okCity = Assert.IsType<OkObjectResult>(resCity);
            var listCity = ((System.Collections.IEnumerable)okCity.Value!).Cast<object>().ToList();
            Assert.Single(listCity);

            // 3. Search by provider name
            var resProvider = await controller.SearchPublicServices(q: "Glow");
            var okProvider = Assert.IsType<OkObjectResult>(resProvider);
            var listProvider = ((System.Collections.IEnumerable)okProvider.Value!).Cast<object>().ToList();
            Assert.Single(listProvider);
        }
    }

    [Fact]
    public async Task SearchPublicServices_EnforcesBoundedLimits()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Omni Spa",
            Slug = "omni-spa",
            City = "Surat",
            IsActive = true,
            IsPublished = true,
            Currency = "INR"
        };

        var catId = Guid.NewGuid();
        var services = Enumerable.Range(1, 15).Select(i => new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            CategoryId = catId,
            Name = $"Service Offering {i:D2}",
            DurationMinutes = 30,
            Price = 1000m * i,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        }).ToList();

        using (var db = CreateDbContext(dbName))
        {
            db.Tenants.Add(tenant);
            db.Services.AddRange(services);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName))
        {
            var controller = CreateDiscoveryController(db);

            // Request limit 4
            var res = await controller.SearchPublicServices(q: "Offering", limit: 4);
            var ok = Assert.IsType<OkObjectResult>(res);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Equal(4, list.Count);
        }
    }

    [Fact]
    public async Task CustomerSearch_ScopedStrictlyToActiveTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var customerA = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            FirstName = "Aarav",
            LastName = "Patel",
            Email = "aarav@example.com",
            Phone = "+919876543210"
        };

        var customerB = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB,
            FirstName = "Aarav", // Same first name to test search query isolation
            LastName = "Sharma",
            Email = "aarav.sharma@example.com",
            Phone = "+919876543211"
        };

        using (var db = CreateDbContext(dbName))
        {
            db.Customers.AddRange(customerA, customerB);
            await db.SaveChangesAsync();
        }

        // Execute search under Tenant A context
        var tenantContextA = new TenantContext();
        tenantContextA.SetTenantId(tenantA);

        using (var dbA = new BooklineDbContext(
            new DbContextOptionsBuilder<BooklineDbContext>().UseInMemoryDatabase(dbName).Options,
            tenantContextA))
        {
            var handler = new CustomerCommandHandler(dbA, tenantContextA);
            var result = await handler.Handle(new GetCustomersQuery("Aarav", false, 1, 10), CancellationToken.None);

            // Must ONLY return Customer A, not Customer B
            Assert.Single(result.Items);
            Assert.Equal(customerA.Id, result.Items[0].Id);
            Assert.Equal("Patel", result.Items[0].LastName);
        }
    }

    [Fact]
    public async Task GetProviderStorefront_ExcludesUnpublishedProvider()
    {
        var dbName = Guid.NewGuid().ToString();
        var unpublishedTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Unpublished Clinic",
            Slug = "unpublished-clinic",
            IsActive = true,
            IsPublished = false // Incomplete onboarding
        };

        using (var db = CreateDbContext(dbName))
        {
            db.Tenants.Add(unpublishedTenant);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName))
        {
            var controller = CreateDiscoveryController(db);
            var result = await controller.GetProviderStorefront("unpublished-clinic");

            // Must return 404 NotFound
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}

// Dummy Test Doubles for DiscoveryController constructor
internal class FakeProviderSearchService : IProviderSearchService
{
    public Task<ProviderSearchResponse> SearchProvidersAsync(
        ProviderSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProviderSearchResponse(
            new List<ProviderCardDto>(), 0, 1, 10, 0));
    }
}

internal class FakeProviderDiscoveryService : IProviderDiscoveryService
{
    public Task<Bookline.Application.Discovery.Dtos.DiscoveryResultResponse> DiscoverProvidersAsync(
        Bookline.Application.Discovery.Dtos.DiscoveryQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Bookline.Application.Discovery.Dtos.DiscoveryResultResponse(
            new List<Bookline.Application.Discovery.Dtos.ProviderLocationResult>(),
            null, 0, 1, 10, 0, null, "Recommended"));
    }
}

internal class FakeLocationDiscoveryService : ILocationDiscoveryService
{
    public Task<IReadOnlyList<Bookline.Application.Discovery.Dtos.PlaceSuggestionDto>> SearchPlacesAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Bookline.Application.Discovery.Dtos.PlaceSuggestionDto>>(new List<Bookline.Application.Discovery.Dtos.PlaceSuggestionDto>());

    public Task<Bookline.Application.Discovery.Dtos.PlaceSuggestionDto?> GetPlaceByIdAsync(string placeId, CancellationToken cancellationToken = default)
        => Task.FromResult<Bookline.Application.Discovery.Dtos.PlaceSuggestionDto?>(null);

    public Task<Bookline.Application.Discovery.Dtos.ResolvedLocationResult> ResolveLocationAsync(Bookline.Application.Discovery.Dtos.LocationResolutionRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new Bookline.Application.Discovery.Dtos.ResolvedLocationResult(
            LocationResolutionType.SelectedCityWithBounds, null, null, null, 10000, false, "ahmedabad", "Ahmedabad", null, null, "City matched"));

    public Task<IReadOnlyList<Bookline.Application.Discovery.Dtos.CitySummaryDto>> GetFeaturedCitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Bookline.Application.Discovery.Dtos.CitySummaryDto>>(new List<Bookline.Application.Discovery.Dtos.CitySummaryDto>());
}

internal class FakeGeocodingProvider : IGeocodingProvider
{
    public Task<GeocodedLocation?> GeocodeAsync(string addressOrCity, CancellationToken cancellationToken = default)
        => Task.FromResult<GeocodedLocation?>(null);

    public Task<string?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>("Ahmedabad");
}
