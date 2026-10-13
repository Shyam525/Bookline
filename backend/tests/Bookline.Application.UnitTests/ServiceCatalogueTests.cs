using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Models;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Application.Services.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ServiceCatalogueTests
{
    private (BooklineDbContext Db, TenantContext TenantCtx) CreateTestContext(Guid? tenantId = null)
    {
        var activeTenantId = tenantId ?? Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(activeTenantId);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"ServiceCatalogueDb_{Guid.NewGuid()}")
            .Options;

        var db = new BooklineDbContext(options, tenantContext);
        return (db, tenantContext);
    }

    [Fact]
    public async Task CreateService_WithValidParameters_CreatesServiceWithSlugAndNavigations()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Hair Care",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var staff1 = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Aarav Sharma",
            IsActive = true
        };
        db.Staff.Add(staff1);

        var location1 = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Main Branch",
            IsActive = true
        };
        db.Locations.Add(location1);

        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Deluxe Hair Spa",
            Slug: "deluxe-hair-spa",
            Description: "Intensive revitalizing treatment",
            DurationMinutes: 60,
            BufferBeforeMinutes: 5,
            BufferAfterMinutes: 10,
            Price: 1500m,
            Currency: "INR",
            IsOnlineBookingEnabled: true,
            IsPublished: true,
            StaffIds: new List<Guid> { staff1.Id },
            LocationIds: new List<Guid> { location1.Id }
        );

        var result = await handler.Handle(new CreateServiceCommand(request), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Deluxe Hair Spa", result.Name);
        Assert.Equal("deluxe-hair-spa", result.Slug);
        Assert.Equal(60, result.DurationMinutes);
        Assert.Equal(5, result.BufferBeforeMinutes);
        Assert.Equal(10, result.BufferAfterMinutes);
        Assert.Equal(75, result.TotalDurationMinutes);
        Assert.Equal(1500m, result.Price);
        Assert.Equal("INR", result.Currency);
        Assert.True(result.IsPublished);
        Assert.Equal("Active", result.Status);
        Assert.Single(result.StaffIds);
        Assert.Equal(staff1.Id, result.StaffIds[0]);
        Assert.Single(result.LocationIds);
        Assert.Equal(location1.Id, result.LocationIds[0]);
    }

    [Theory]
    [InlineData(0, 0, 0, 100)]    // Duration 0
    [InlineData(-10, 0, 0, 100)]  // Negative duration
    [InlineData(30, -5, 0, 100)]  // Negative buffer before
    [InlineData(30, 0, -5, 100)]  // Negative buffer after
    [InlineData(30, 0, 0, -50)]   // Negative price
    public async Task CreateService_WithInvalidValues_ThrowsValidationException(
        int duration, int bufferBefore, int bufferAfter, decimal price)
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Skin Care",
            IsActive = true
        };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Test Facial",
            DurationMinutes: duration,
            BufferBeforeMinutes: bufferBefore,
            BufferAfterMinutes: bufferAfter,
            Price: price
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task CreateService_WithStaffFromOtherTenant_ThrowsValidationException()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Spa",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        // Staff belonging to a different tenant
        var foreignStaff = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Foreign Specialist",
            IsActive = true
        };
        db.Staff.Add(foreignStaff);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Cross Tenant Attack Service",
            StaffIds: new List<Guid> { foreignStaff.Id }
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task CreateService_WithLocationFromOtherTenant_ThrowsValidationException()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Wellness",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        // Location belonging to a different tenant
        var foreignLocation = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Foreign Branch",
            IsActive = true
        };
        db.Locations.Add(foreignLocation);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Location Spoofing Service",
            LocationIds: new List<Guid> { foreignLocation.Id }
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateService_UpdatesFieldsAndAssociations()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Massage",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var staff1 = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Staff 1",
            IsActive = true
        };
        var staff2 = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Staff 2",
            IsActive = true
        };
        db.Staff.AddRange(staff1, staff2);
        await db.SaveChangesAsync();

        var created = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Swedish Massage",
            Price: 2000m,
            StaffIds: new List<Guid> { staff1.Id }
        )), CancellationToken.None);

        // Update to assign staff2 instead and change price/name
        var updateRequest = new UpdateServiceRequest(
            CategoryId: category.Id,
            Name: "Deep Tissue Massage",
            Price: 2500m,
            StaffIds: new List<Guid> { staff2.Id },
            IsPublished: true
        );

        var updated = await handler.Handle(new UpdateServiceCommand(created.Id, updateRequest), CancellationToken.None);

        Assert.Equal("Deep Tissue Massage", updated.Name);
        Assert.Equal(2500m, updated.Price);
        Assert.Single(updated.StaffIds);
        Assert.Equal(staff2.Id, updated.StaffIds[0]);
    }

    [Fact]
    public async Task ArchiveService_MarksServiceInactiveAndArchived()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Barber",
            IsActive = true
        };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var created = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Beard Trim",
            Price: 300m
        )), CancellationToken.None);

        var archived = await handler.Handle(new ArchiveServiceCommand(created.Id), CancellationToken.None);
        Assert.True(archived);

        var serviceInDb = await db.Services.FirstAsync(s => s.Id == created.Id);
        Assert.True(serviceInDb.IsArchived);
        Assert.False(serviceInDb.IsActive);
        Assert.Equal("Archived", serviceInDb.Status);
    }

    [Fact]
    public async Task PublicStorefront_ExcludesUnpublishedAndInactiveServices()
    {
        var (db, tenantCtx) = CreateTestContext();
        tenantCtx.EnableSystemMode();

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Luxe Salon",
            Slug = "luxe-salon",
            Category = "Beauty",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            City = "Ahmedabad",
            Address = "Vastrapur",
            Currency = "INR"
        };
        db.Tenants.Add(tenant);

        var publishedService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Public Facial",
            Slug = "public-facial",
            Price = 1000m,
            DurationMinutes = 45,
            IsActive = true,
            IsPublished = true,
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        var unpublishedService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Draft Secret Treatment",
            Slug = "draft-secret",
            Price = 5000m,
            DurationMinutes = 60,
            IsActive = true,
            IsPublished = false, // Unpublished!
            IsArchived = false,
            IsOnlineBookingEnabled = true
        };

        var archivedService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Old Deprecated Service",
            Slug = "old-service",
            Price = 500m,
            DurationMinutes = 30,
            IsActive = false,
            IsPublished = true,
            IsArchived = true, // Archived!
            IsOnlineBookingEnabled = false
        };

        db.Services.AddRange(publishedService, unpublishedService, archivedService);
        await db.SaveChangesAsync();

        var storefrontService = new PublicStorefrontService(db);
        var publicServices = await storefrontService.GetServicesBySlugAsync("luxe-salon");

        Assert.NotNull(publicServices);
        Assert.Single(publicServices);
        Assert.Equal("Public Facial", publicServices[0].Name);

        // Check single service by slug
        var singlePublic = await storefrontService.GetServiceBySlugAsync("luxe-salon", "public-facial");
        Assert.NotNull(singlePublic);
        Assert.Equal("Public Facial", singlePublic.Name);

        var singleUnpublished = await storefrontService.GetServiceBySlugAsync("luxe-salon", "draft-secret");
        Assert.Null(singleUnpublished);
    }
}
