using Bookline.Application.Common.Models;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Application.Tenants.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.UnitTests;

public class ProviderOnboardingSection110Tests
{
    private (BooklineDbContext DbContext, TenantContext TenantContext) CreateContext()
    {
        var tenantContext = new TenantContext();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        return (context, tenantContext);
    }

    [Fact]
    public async Task Section110_Step1_AccountAndMembership_PersistsOwnerAndMembership()
    {
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Aura Studio",
            Slug = "aura-studio",
            OnboardingStep = 1,
            IsPublished = false,
            IsOnboardingCompleted = false
        };
        var ownerUser = new AppUser
        {
            Id = ownerUserId,
            TenantId = tenantId,
            Email = "elena@aurastudio.com",
            FirstName = "Elena",
            LastName = "Vance",
            Role = "Owner",
            Phone = "+1 555 0100"
        };
        db.Tenants.Add(tenant);
        db.Users.Add(ownerUser);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Save Step 1 with updated owner name and phone
        var status = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 1,
            OwnerName: "Elena Vance-Brody",
            OwnerPhone: "+1 555 9999"
        )), CancellationToken.None);

        Assert.Equal("Elena Vance-Brody", status.OwnerName);
        Assert.Equal("+1 555 9999", status.OwnerPhone);
        Assert.Equal("Owner", status.MembershipRole);

        // Verify database state: AppUser updated and OrganizationMembership created
        var updatedUser = await db.Users.FirstAsync(u => u.Id == ownerUserId);
        Assert.Equal("Elena", updatedUser.FirstName);
        Assert.Equal("Vance-Brody", updatedUser.LastName);
        Assert.Equal("+1 555 9999", updatedUser.Phone);

        var membership = await db.OrganizationMemberships.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == ownerUserId);
        Assert.NotNull(membership);
        Assert.Equal("Owner", membership.Role);
        Assert.True(membership.IsActive);
    }

    [Fact]
    public async Task Section110_Step8_ProductsIsOptional_CanBeSkippedWithoutError()
    {
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Pure Wellness",
            Slug = "pure-wellness",
            OnboardingStep = 8
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Save Step 8 with null / empty product fields (skipping optional retail)
        var status = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 8,
            FirstProductName: null,
            FirstProductPrice: null
        )), CancellationToken.None);

        // Verify progress advanced without throwing or creating dummy products
        Assert.True(status.CurrentStep >= 9);
        var productCount = await db.Products.CountAsync(p => p.TenantId == tenantId);
        Assert.Equal(0, productCount);
    }

    [Fact]
    public async Task Section110_HandlesDuplicateSubmissionsSafely_WithoutCreatingDuplicateRecords()
    {
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Serene Salon",
            Slug = "serene-salon",
            OnboardingStep = 5
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // 1. Submit Step 5 (Location) first time
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 5,
            LocationName: "Serene Studio Branch 1",
            Address: "123 Main St",
            City: "Seattle"
        )), CancellationToken.None);

        // 2. Submit Step 5 again with updated address (simulating user edit or duplicate request)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 5,
            LocationName: "Serene Studio Flagship",
            Address: "456 Market St",
            City: "Seattle"
        )), CancellationToken.None);

        // Verify only 1 location exists and has updated data
        var locations = await db.Locations.Where(l => l.TenantId == tenantId).ToListAsync();
        Assert.Single(locations);
        Assert.Equal("Serene Studio Flagship", locations[0].Name);
        Assert.Equal("456 Market St", locations[0].Address);

        // 3. Submit Step 7 (Services) multiple times
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 7,
            FirstServiceName: "Classic Facial",
            FirstServiceDurationMinutes: 45,
            FirstServicePrice: 80m
        )), CancellationToken.None);

        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 7,
            FirstServiceName: "Signature Deluxe Facial",
            FirstServiceDurationMinutes: 60,
            FirstServicePrice: 120m
        )), CancellationToken.None);

        var services = await db.Services.Where(s => s.TenantId == tenantId).ToListAsync();
        Assert.Single(services);
        Assert.Equal("Signature Deluxe Facial", services[0].Name);
        Assert.Equal(120m, services[0].Price);
    }

    [Fact]
    public async Task Section110_ResumeSavedProgress_WhenProviderReturns()
    {
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        // Simulate provider who completed up to Step 6, left, and returns
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Resumed Aesthetics",
            Slug = "resumed-aesthetics",
            BusinessType = "Aesthetic Clinic",
            Description = "Clinical grade skin rejuvenation.",
            Address = "700 Medical Arts Tower",
            City = "Chicago",
            State = "IL",
            PostalCode = "60601",
            Country = "USA",
            TimeZoneId = "America/Chicago",
            Currency = "USD",
            OnboardingStep = 7,
            HoldDurationMinutes = 5,
            BookingHorizonDays = 30
        };
        db.Tenants.Add(tenant);
        db.Locations.Add(new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Aesthetics Main",
            Address = "700 Medical Arts Tower",
            City = "Chicago",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Fetch onboarding status (simulating page load after re-login)
        var status = await handler.Handle(new GetOnboardingStatusQuery(), CancellationToken.None);

        Assert.Equal(7, status.CurrentStep);
        Assert.Equal("Resumed Aesthetics", status.BusinessName);
        Assert.Equal("Aesthetic Clinic", status.BusinessType);
        Assert.Equal("700 Medical Arts Tower", status.Address);
        Assert.Equal("Chicago", status.City);
        Assert.Equal("America/Chicago", status.TimeZoneId);
        Assert.Equal("USD", status.Currency);

        // Completed steps dynamically evaluated from persisted records
        Assert.Contains(1, status.CompletedSteps);
        Assert.Contains(2, status.CompletedSteps);
        Assert.Contains(3, status.CompletedSteps);
        Assert.Contains(4, status.CompletedSteps);
        Assert.Contains(5, status.CompletedSteps);
        Assert.Contains(6, status.CompletedSteps);
        Assert.DoesNotContain(7, status.CompletedSteps); // Services not yet saved
    }

    [Fact]
    public async Task Section110_ReadinessChecks_PreventPublicationOfInactiveOrRejectedTenant()
    {
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Deactivated Org",
            Slug = "deactivated-org",
            BusinessType = "Spa",
            Description = "Spa treatments",
            Address = "123 Street",
            TimeZoneId = "UTC",
            Currency = "USD",
            IsActive = false, // Deactivated!
            IsPublished = false
        };
        db.Tenants.Add(tenant);
        db.Locations.Add(new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Main", Address = "123 Street", IsActive = true });
        db.Services.Add(new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Massage", Price = 50m, IsActive = true, IsPublished = true });
        db.Staff.Add(new DomainStaff { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Staff A", IsActive = true });
        db.WorkingHours.Add(new WorkingHours { TenantId = tenantId, StaffId = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        var publishResult = await handler.Handle(new PublishStorefrontCommand(), CancellationToken.None);

        Assert.False(publishResult.Success);
        Assert.False(publishResult.IsPublished);
        Assert.Contains("inactive or rejected", publishResult.Message);
    }
}
