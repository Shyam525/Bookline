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

    [Theory]
    [InlineData("invalid slug with spaces")]
    [InlineData("slug_with_underscores")]
    [InlineData("slug--with--double-hyphens")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("../path-traversal")]
    public async Task GetStorefrontBySlugAsync_WhenSlugFormatIsInvalid_ReturnsNull(string invalidSlug)
    {
        using var db = CreateInMemoryDbContext();
        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync(invalidSlug);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(ModerationStatus.Rejected)]
    [InlineData(ModerationStatus.Suspended)]
    public async Task GetStorefrontBySlugAsync_WhenModerationStatusRejectedOrSuspended_ReturnsNull(ModerationStatus status)
    {
        using var db = CreateInMemoryDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Restricted Venue",
            Slug = "restricted-venue",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = status
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync("restricted-venue");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStorefrontBySlugAsync_IncludesApprovedReviewTitleAndProviderResponse()
    {
        using var db = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Glow Studio",
            Slug = "glow-studio",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved
        };
        db.Tenants.Add(tenant);

        var review = new Review
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerName = "Alice Morgan",
            Rating = 5,
            Title = "Incredible Experience",
            Comment = "Best service in town! Super professional.",
            ProviderResponse = "Thank you Alice, we loved having you!",
            ModerationStatus = ModerationStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var result = await serviceInstance.GetStorefrontBySlugAsync("glow-studio");

        Assert.NotNull(result);
        Assert.Single(result.Reviews);
        Assert.Equal("Incredible Experience", result.Reviews[0].Title);
        Assert.Equal("Thank you Alice, we loved having you!", result.Reviews[0].ProviderResponse);
    }

    [Fact]
    public async Task GetHoursBySlugAsync_CalculatesVenueMinMaxAndMarksClosedDays()
    {
        using var db = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Time Master",
            Slug = "time-master",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved
        };
        db.Tenants.Add(tenant);

        var staff1Id = Guid.NewGuid();
        var staff2Id = Guid.NewGuid();

        // Staff 1 works Monday 09:00 - 15:00
        var wh1 = new WorkingHours
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StaffId = staff1Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(15, 0)
        };
        // Staff 2 works Monday 12:00 - 19:30
        var wh2 = new WorkingHours
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StaffId = staff2Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(19, 30)
        };
        db.WorkingHours.AddRange(wh1, wh2);
        await db.SaveChangesAsync();

        var serviceInstance = new PublicStorefrontService(db);
        var hours = await serviceInstance.GetHoursBySlugAsync("time-master");

        Assert.NotNull(hours);
        Assert.Equal(7, hours.Count);

        var monday = hours.First(h => h.DayOfWeek == DayOfWeek.Monday.ToString());
        Assert.False(monday.IsClosed);
        Assert.Equal("09:00", monday.OpenTime);
        Assert.Equal("19:30", monday.CloseTime);

        var tuesday = hours.First(h => h.DayOfWeek == DayOfWeek.Tuesday.ToString());
        Assert.True(tuesday.IsClosed);
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
