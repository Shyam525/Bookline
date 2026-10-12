using System.Reflection;
using Bookline.Application.Common.Models;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class PublicStorefrontApiTests
{
    private BooklineDbContext CreateInMemoryDbContext()
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"StorefrontTestDb_{Guid.NewGuid()}")
            .Options;
        return new BooklineDbContext(options, tenantContext);
    }



    [Fact]
    public async Task GetStorefrontBySlugAsync_WhenPublishedAndActive_ReturnsStorefront()
    {
        using var db = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Studio Velvet",
            Slug = "studio-velvet",
            Category = "Beauty & Hair",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            City = "Ahmedabad",
            Address = "Bodakdev",
            Currency = "INR"
        };
        db.Tenants.Add(tenant);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Signature Haircut",
            Price = 1200m,
            DurationMinutes = 45,
            IsActive = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync("studio-velvet");

        Assert.NotNull(result);
        Assert.Equal("Studio Velvet", result.Provider.Name);
        Assert.Equal("studio-velvet", result.Provider.Slug);
        Assert.Single(result.Services);
        Assert.Equal("Signature Haircut", result.Services[0].Name);
    }

    [Fact]
    public async Task GetStorefrontBySlugAsync_WhenUnpublished_ReturnsNull()
    {
        using var db = CreateInMemoryDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Secret Draft Spa",
            Slug = "secret-draft-spa",
            IsActive = true,
            IsPublished = false, // Unpublished!
            VerificationStatus = VerificationStatus.Pending
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync("secret-draft-spa");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStorefrontBySlugAsync_WhenSuspended_ReturnsNull()
    {
        using var db = CreateInMemoryDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Suspended Venue",
            Slug = "suspended-venue",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Suspended // Suspended!
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync("suspended-venue");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetServicesBySlugAsync_OnlyReturnsActiveUnarchivedServices()
    {
        using var db = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Elite Spa",
            Slug = "elite-spa",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified
        };
        db.Tenants.Add(tenant);

        var activeService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Facial Glow",
            Price = 900m,
            IsActive = true,
            IsArchived = false
        };
        var archivedService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Old Discontinued Scrub",
            Price = 400m,
            IsActive = true,
            IsArchived = true // Archived
        };
        db.Services.AddRange(activeService, archivedService);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var services = await serviceInstance.GetServicesBySlugAsync("elite-spa");

        Assert.NotNull(services);
        Assert.Single(services);
        Assert.Equal("Facial Glow", services[0].Name);
    }

    [Fact]
    public void PublicStorefrontDtos_NeverLeakSensitiveInternalFields()
    {
        var sensitivePropertyNames = new[]
        {
            "Password",
            "PasswordHash",
            "InternalNotes",
            "SuspensionReason",
            "CommissionRate",
            "Payout",
            "StripeAccount",
            "Bank",
            "Secret",
            "IsArchived",
            "DeletedAt"
        };

        var publicTypes = new[]
        {
            typeof(PublicStorefrontResponse),
            typeof(PublicProviderProfileDto),
            typeof(PublicServiceDto),
            typeof(PublicLocationDto),
            typeof(PublicProductDto),
            typeof(PublicStaffDto),
            typeof(PublicReviewDto),
            typeof(PublicBusinessHoursDto)
        };

        foreach (var type in publicTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                foreach (var forbidden in sensitivePropertyNames)
                {
                    Assert.False(
                        prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                        $"Public storefront DTO '{type.Name}' leaks forbidden internal property '{prop.Name}' matching '{forbidden}'."
                    );
                }
            }
        }
    }
}
