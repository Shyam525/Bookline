using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Application.Services.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ServiceManagementSection25Tests
{
    private (BooklineDbContext Db, TenantContext TenantCtx) CreateTestContext(Guid? tenantId = null)
    {
        var activeTenantId = tenantId ?? Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(activeTenantId);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"ServiceSection25Db_{Guid.NewGuid()}")
            .Options;

        var db = new BooklineDbContext(options, tenantContext);
        return (db, tenantContext);
    }

    [Fact]
    public async Task CreateService_CalculatesCustomerFacingAndTotalDurationExplicitly()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Spa", IsActive = true };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Therapeutic Massage",
            DurationMinutes: 45,
            BufferBeforeMinutes: 10,
            BufferAfterMinutes: 15,
            Price: 120.00m,
            Currency: "USD"
        );

        var result = await handler.Handle(new CreateServiceCommand(request), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Therapeutic Massage", result.Name);
        Assert.Equal(45, result.DurationMinutes);
        Assert.Equal(10, result.BufferBeforeMinutes);
        Assert.Equal(15, result.BufferAfterMinutes);
        Assert.Equal(70, result.TotalDurationMinutes); // 45 + 10 + 15 = 70 min occupancy
        Assert.Equal(120.00m, result.Price);
        Assert.Equal("USD", result.Currency);
        Assert.True(result.IsActive);
        Assert.Equal("Active", result.Status);
    }

    [Theory]
    [InlineData("", 30, 0, 0, 50, "USD")]             // Empty name
    [InlineData("Valid", 0, 0, 0, 50, "USD")]          // 0 duration
    [InlineData("Valid", -15, 0, 0, 50, "USD")]        // Negative duration
    [InlineData("Valid", 30, -5, 0, 50, "USD")]        // Negative buffer before
    [InlineData("Valid", 30, 0, -10, 50, "USD")]       // Negative buffer after
    [InlineData("Valid", 30, 0, 0, -5, "USD")]         // Negative price
    [InlineData("Valid", 30, 0, 0, 50, "")]            // Empty currency
    [InlineData("Valid", 30, 0, 0, 50, "US")]          // Invalid currency length
    public async Task CreateService_InvalidParameters_ThrowsValidationException(
        string name, int duration, int bufferBefore, int bufferAfter, decimal price, string currency)
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "General", IsActive = true };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: name,
            DurationMinutes: duration,
            BufferBeforeMinutes: bufferBefore,
            BufferAfterMinutes: bufferAfter,
            Price: price,
            Currency: currency
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task CreateService_WithCrossTenantResource_ThrowsValidationException()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Wellness", IsActive = true };
        db.ServiceCategories.Add(category);

        // Resource owned by a foreign business
        var foreignResource = new Resource
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Foreign Treatment Room",
            Type = "Room",
            IsActive = true
        };
        db.Resources.Add(foreignResource);
        await db.SaveChangesAsync();

        var request = new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Hydrotherapy",
            RequiresResource: true,
            RequiredResourceId: foreignResource.Id
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task ConfigureServiceResources_AssociatesResourceSuccessfully()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Dental", IsActive = true };
        var resource = new Resource { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Dental Operatory 1", Type = "Chair", IsActive = true };
        db.ServiceCategories.Add(category);
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var service = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Teeth Whitening"
        )), CancellationToken.None);

        var configured = await handler.Handle(new ConfigureServiceResourcesCommand(
            service.Id,
            RequiresResource: true,
            RequiredResourceType: "Chair",
            RequiredResourceId: resource.Id
        ), CancellationToken.None);

        Assert.True(configured.RequiresResource);
        Assert.Equal("Chair", configured.RequiredResourceType);
        Assert.Equal(resource.Id, configured.RequiredResourceId);
        Assert.Equal("Dental Operatory 1", configured.RequiredResourceName);
    }

    [Fact]
    public async Task SetServiceStatus_TransitionsCorrectlyAcrossLifecycle()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Beauty", IsActive = true };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var created = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Brow Lamination",
            IsPublished: true
        )), CancellationToken.None);

        Assert.Equal("Active", created.Status);
        Assert.True(created.IsActive);
        Assert.True(created.IsPublished);

        // Deactivate
        var deactivated = await handler.Handle(new SetServiceStatusCommand(created.Id, "Inactive"), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
        Assert.False(deactivated.IsActive);

        // Set to Draft
        var draft = await handler.Handle(new SetServiceStatusCommand(created.Id, "Draft"), CancellationToken.None);
        Assert.Equal("Draft", draft.Status);
        Assert.True(draft.IsActive);
        Assert.False(draft.IsPublished);

        // Reactivate
        var reactivated = await handler.Handle(new SetServiceStatusCommand(created.Id, "Active"), CancellationToken.None);
        Assert.Equal("Active", reactivated.Status);
        Assert.True(reactivated.IsActive);
        Assert.True(reactivated.IsPublished);
    }

    [Fact]
    public async Task AssignServiceLocations_WithPriceOverrides_PersistsCorrectly()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Fitness", IsActive = true };
        var loc1 = new Location { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Downtown Studio", IsActive = true };
        var loc2 = new Location { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Uptown Studio", IsActive = true };
        db.ServiceCategories.Add(category);
        db.Locations.AddRange(loc1, loc2);
        await db.SaveChangesAsync();

        var service = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Personal Training",
            Price: 80.00m
        )), CancellationToken.None);

        var overrides = new List<LocationPriceOverrideDto>
        {
            new(loc1.Id, PriceOverride: null, IsAvailable: true),
            new(loc2.Id, PriceOverride: 95.00m, IsAvailable: true) // Price override at premium branch
        };

        var result = await handler.Handle(new AssignServiceLocationsCommand(
            service.Id,
            new List<Guid> { loc1.Id, loc2.Id },
            overrides
        ), CancellationToken.None);

        Assert.Equal(2, result.LocationIds.Count);

        var sl2 = await db.ServiceLocations.FirstAsync(sl => sl.ServiceId == service.Id && sl.LocationId == loc2.Id);
        Assert.Equal(95.00m, sl2.PriceOverride);
    }

    [Fact]
    public async Task ResourceCrud_WorksEndToEnd_AndPreventsDeletingResourceInUse()
    {
        var (db, tenantCtx) = CreateTestContext();
        var handler = new ServiceHandlers(db, tenantCtx);

        // 1. Create Resource
        var created = await handler.Handle(new CreateResourceCommand(new CreateResourceRequest(
            Name: "Laser Room A",
            Description: "Class 4 laser treatment room",
            Type: "Room",
            Capacity: 1
        )), CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Laser Room A", created.Name);
        Assert.True(created.IsActive);

        // 2. Update Resource
        var updated = await handler.Handle(new UpdateResourceCommand(created.Id, new UpdateResourceRequest(
            Name: "Laser Suite Alpha",
            Type: "Room",
            Capacity: 2
        )), CancellationToken.None);
        Assert.Equal("Laser Suite Alpha", updated.Name);
        Assert.Equal(2, updated.Capacity);

        // 3. Link resource to active service
        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Skin", IsActive = true };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync();

        var service = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: category.Id,
            Name: "Laser Hair Removal",
            RequiresResource: true,
            RequiredResourceId: updated.Id
        )), CancellationToken.None);

        // 4. Try deleting resource in use -> Must fail
        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteResourceCommand(updated.Id), CancellationToken.None));

        // 5. Unlink from service
        await handler.Handle(new ConfigureServiceResourcesCommand(service.Id, false, null, null), CancellationToken.None);

        // 6. Now delete resource -> Must succeed
        var deleted = await handler.Handle(new DeleteResourceCommand(updated.Id), CancellationToken.None);
        Assert.True(deleted);
    }

    [Fact]
    public async Task CreateBooking_EnforcesStaffEligibility_RejectsIneligibleStaff()
    {
        var (db, tenantCtx) = CreateTestContext();
        var holdService = new SlotHoldService(redis: null);
        var slotEngine = new SlotEngine();
        var bookingHandler = new CreateBookingCommandHandler(db, tenantCtx, holdService, slotEngine);

        var staff1 = new Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Eligible Staff", IsActive = true };
        var staff2 = new Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Ineligible Staff", IsActive = true };
        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Cuts", IsActive = true };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            CategoryId = category.Id,
            Name = "Haircut",
            DurationMinutes = 30,
            Price = 40m,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        db.Staff.AddRange(staff1, staff2);
        db.ServiceCategories.Add(category);
        db.Services.Add(service);
        // Only staff1 is assigned to this service
        db.StaffServices.Add(new StaffService { TenantId = tenantCtx.TenantId, StaffId = staff1.Id, ServiceId = service.Id });

        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, FirstName = "Bob", Email = "bob@example.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var holdId = await holdService.AcquireHoldAsync(tenantCtx.TenantId, staff2.Id, startUtc, TimeSpan.FromMinutes(30));

        // Attempt booking with staff2 who is NOT assigned to this service
        var command = new CreateBookingCommand(staff2.Id, service.Id, customer.Id, startUtc, holdId!.Value);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            bookingHandler.Handle(command, CancellationToken.None));

        Assert.Contains("not eligible to perform this service", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_EnforcesLocationApplicability_RejectsUnavailableLocation()
    {
        var (db, tenantCtx) = CreateTestContext();
        var holdService = new SlotHoldService(redis: null);
        var slotEngine = new SlotEngine();
        var bookingHandler = new CreateBookingCommandHandler(db, tenantCtx, holdService, slotEngine);

        var staff = new Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Staff A", IsActive = true };
        var locAllowed = new Location { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "City Branch", IsActive = true };
        var locNotAllowed = new Location { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Suburban Branch", IsActive = true };
        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Cuts", IsActive = true };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            CategoryId = category.Id,
            Name = "Deluxe Cut",
            DurationMinutes = 45,
            Price = 60m,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };

        db.Staff.Add(staff);
        db.Locations.AddRange(locAllowed, locNotAllowed);
        db.ServiceCategories.Add(category);
        db.Services.Add(service);
        db.StaffServices.Add(new StaffService { TenantId = tenantCtx.TenantId, StaffId = staff.Id, ServiceId = service.Id });
        // Only locAllowed is configured
        db.ServiceLocations.Add(new ServiceLocation { TenantId = tenantCtx.TenantId, ServiceId = service.Id, LocationId = locAllowed.Id, IsAvailable = true });

        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, FirstName = "Jane", Email = "jane@example.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var holdId = await holdService.AcquireHoldAsync(tenantCtx.TenantId, staff.Id, startUtc, TimeSpan.FromMinutes(45));

        // Attempt booking at locNotAllowed
        var command = new CreateBookingCommand(staff.Id, service.Id, customer.Id, startUtc, holdId!.Value, LocationId: locNotAllowed.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            bookingHandler.Handle(command, CancellationToken.None));

        Assert.Contains("not available at the selected location", ex.Message);
    }

    [Fact]
    public async Task CreateBooking_EnforcesResourceOccupancyConflict_IncludingBuffers()
    {
        var (db, tenantCtx) = CreateTestContext();
        var holdService = new SlotHoldService(redis: null);
        var slotEngine = new SlotEngine();
        var bookingHandler = new CreateBookingCommandHandler(db, tenantCtx, holdService, slotEngine);

        var staff1 = new Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Doctor 1", IsActive = true };
        var staff2 = new Domain.Entities.Staff { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Doctor 2", IsActive = true };
        var resource = new Resource { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Surgery Suite 1", Type = "Room", IsActive = true };
        var category = new ServiceCategory { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, Name = "Surgery", IsActive = true };

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            CategoryId = category.Id,
            Name = "Minor Procedure",
            DurationMinutes = 60,
            BufferBeforeMinutes = 15,
            BufferAfterMinutes = 15,
            Price = 500m,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true,
            RequiresResource = true,
            RequiredResourceId = resource.Id
        };

        db.Staff.AddRange(staff1, staff2);
        db.Resources.Add(resource);
        db.ServiceCategories.Add(category);
        db.Services.Add(service);
        db.StaffServices.AddRange(
            new StaffService { TenantId = tenantCtx.TenantId, StaffId = staff1.Id, ServiceId = service.Id },
            new StaffService { TenantId = tenantCtx.TenantId, StaffId = staff2.Id, ServiceId = service.Id }
        );

        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantCtx.TenantId, FirstName = "Pat", Email = "pat@example.com" };
        db.Customers.Add(customer);

        // Pre-existing booking occupying the resource from 10:00 to 11:00
        var baseTime = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(10);
        var existingBooking = new Booking(tenantCtx.TenantId, staff1.Id, service.Id, customer.Id, baseTime, baseTime.AddHours(1))
        {
            ResourceId = resource.Id
        };
        existingBooking.Confirm();
        db.Bookings.Add(existingBooking);
        await db.SaveChangesAsync();

        // Second doctor tries to book at 11:05 (5 min after the first booking ended).
        // Since service requires 15 min buffer before and first service has 15 min buffer after,
        // occupancy for second booking starts at 11:05 - 15 min = 10:50, overlapping the first booking!
        var conflictingStart = baseTime.AddMinutes(65);
        var holdId = await holdService.AcquireHoldAsync(tenantCtx.TenantId, staff2.Id, conflictingStart, TimeSpan.FromMinutes(60));

        var command = new CreateBookingCommand(staff2.Id, service.Id, customer.Id, conflictingStart, holdId!.Value, ResourceId: resource.Id);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            bookingHandler.Handle(command, CancellationToken.None));

        Assert.Contains("already reserved for this time window", ex.Message);
    }
}
