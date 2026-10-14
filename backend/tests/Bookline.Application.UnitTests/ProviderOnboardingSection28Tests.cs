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

public class ProviderOnboardingSection28Tests
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
    public async Task OnboardingFlow_PersistsDataToRealDatabaseEntities_AtEveryStep()
    {
        // Section 28: Each step must persist its data to the backend.
        // Do not simulate progression using only component state or browser storage.
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Initial Studio",
            Slug = "initial-studio",
            OnboardingStep = 1,
            IsPublished = false,
            IsOnboardingCompleted = false
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Step 2 — Business creation
        var step2Result = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 2,
            BusinessName: "Luxe Glow Salon",
            BusinessSlug: "luxe-glow-salon"
        )), CancellationToken.None);
        Assert.Equal("Luxe Glow Salon", (await db.Tenants.FirstAsync(t => t.Id == tenantId)).Name);
        Assert.Equal("luxe-glow-salon", (await db.Tenants.FirstAsync(t => t.Id == tenantId)).Slug);

        // Step 3 — Business type
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 3,
            BusinessType: "Hair & Beauty Salon",
            Category: "Wellness"
        )), CancellationToken.None);
        Assert.Equal("Hair & Beauty Salon", (await db.Tenants.FirstAsync(t => t.Id == tenantId)).BusinessType);

        // Step 4 — Business profile
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 4,
            Description: "Boutique scalp therapies and bespoke hair styling.",
            Website: "https://luxeglow.example.com",
            Phone: "+91 98250 11223"
        )), CancellationToken.None);
        Assert.Equal("Boutique scalp therapies and bespoke hair styling.", (await db.Tenants.FirstAsync(t => t.Id == tenantId)).Description);

        // Step 5 — First location (Persisted into Location table)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 5,
            LocationName: "Luxe Glow Flagship",
            Address: "402 Bodakdev Hub, SG Highway",
            City: "Ahmedabad",
            State: "Gujarat",
            PostalCode: "380054",
            Country: "India"
        )), CancellationToken.None);
        var location = await db.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId);
        Assert.NotNull(location);
        Assert.Equal("402 Bodakdev Hub, SG Highway", location.Address);
        Assert.Equal("Ahmedabad", location.City);
        Assert.True(location.IsActive);

        // Step 6 — Timezone and currency
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 6,
            TimeZoneId: "Asia/Kolkata",
            Currency: "INR"
        )), CancellationToken.None);
        var tenantAfterStep6 = await db.Tenants.FirstAsync(t => t.Id == tenantId);
        Assert.Equal("Asia/Kolkata", tenantAfterStep6.TimeZoneId);
        Assert.Equal("INR", tenantAfterStep6.Currency);
        // Location timezone and currency synced
        var updatedLocation = await db.Locations.FirstAsync(l => l.TenantId == tenantId);
        Assert.Equal("Asia/Kolkata", updatedLocation.Timezone);
        Assert.Equal("INR", updatedLocation.Currency);

        // Step 7 — Add services (Persisted into Services table)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 7,
            FirstServiceName: "Signature Haircut & Spa",
            FirstServiceDurationMinutes: 60,
            FirstServicePrice: 1200m
        )), CancellationToken.None);
        var service = await db.Services.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        Assert.NotNull(service);
        Assert.Equal("Signature Haircut & Spa", service.Name);
        Assert.Equal(60, service.DurationMinutes);
        Assert.Equal(1200m, service.Price);
        Assert.True(service.IsActive);
        Assert.True(service.IsOnlineBookingEnabled);

        // Step 8 — Add products (Persisted into Products table)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 8,
            FirstProductName: "Argan Scalp Elixir",
            FirstProductPrice: 750m,
            FirstProductStock: 30,
            FirstProductSku: "ELX-001"
        )), CancellationToken.None);
        var product = await db.Products.FirstOrDefaultAsync(p => p.TenantId == tenantId);
        Assert.NotNull(product);
        Assert.Equal("Argan Scalp Elixir", product.Name);
        Assert.Equal(750m, product.Price);
        Assert.Equal(30, product.StockQuantity);

        // Step 9 — Add staff (Persisted into Staff table and linked to service)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 9,
            FirstStaffName: "Marcus Brody",
            FirstStaffTitle: "Senior Master Stylist"
        )), CancellationToken.None);
        var staff = await db.Staff.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        Assert.NotNull(staff);
        Assert.Equal("Marcus Brody", staff.Name);
        Assert.Equal("Senior Master Stylist", staff.Title);
        // Linked in StaffServices
        Assert.True(await db.StaffServices.AnyAsync(ss => ss.StaffId == staff.Id && ss.ServiceId == service.Id));

        // Step 10 — Configure working hours (Persisted into WorkingHours table)
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 10,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(18, 0),
            WorkingDays: new List<int> { 1, 2, 3, 4, 5, 6 }
        )), CancellationToken.None);
        var workingHours = await db.WorkingHours.Where(w => w.TenantId == tenantId).ToListAsync();
        Assert.Equal(6, workingHours.Count);
        Assert.All(workingHours, w => Assert.Equal(new TimeOnly(9, 0), w.StartTime));

        // Step 11 — Configure booking rules
        await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            Step: 11,
            HoldDurationMinutes: 5,
            MinimumNoticeHours: 3,
            BookingHorizonDays: 45
        )), CancellationToken.None);
        var tenantAfterRules = await db.Tenants.FirstAsync(t => t.Id == tenantId);
        Assert.Equal(5, tenantAfterRules.HoldDurationMinutes);
        Assert.Equal(3, tenantAfterRules.MinimumNoticeHours);
        Assert.Equal(45, tenantAfterRules.BookingHorizonDays);
    }

    [Fact]
    public async Task SaveAndResume_RecoversExactPersistedState_FromDatabase()
    {
        // Section 28: Support save-and-resume. Recover persisted onboarding progress.
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Resumed Studio",
            Slug = "resumed-studio",
            BusinessType = "Boutique Spa",
            Description = "Relaxing aromatherapies",
            Address = "100 Lotus Park",
            City = "Bangalore",
            TimeZoneId = "Asia/Kolkata",
            Currency = "INR",
            OnboardingStep = 6,
            HoldDurationMinutes = 5,
            BookingHorizonDays = 30
        };
        db.Tenants.Add(tenant);
        db.Locations.Add(new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Lotus Studio",
            Address = "100 Lotus Park",
            City = "Bangalore",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Fetch onboarding status (simulating page reload / session recovery)
        var status = await handler.Handle(new GetOnboardingStatusQuery(), CancellationToken.None);

        Assert.Equal("Resumed Studio", status.BusinessName);
        Assert.Equal("Boutique Spa", status.BusinessType);
        Assert.Equal("100 Lotus Park", status.Address);
        Assert.Equal("Bangalore", status.City);
        Assert.Equal("Asia/Kolkata", status.TimeZoneId);
        Assert.Equal("INR", status.Currency);
        Assert.Equal(6, status.CurrentStep);
        Assert.False(status.IsCompleted);

        // Dynamic completed steps evaluation
        Assert.Contains(1, status.CompletedSteps);
        Assert.Contains(2, status.CompletedSteps);
        Assert.Contains(3, status.CompletedSteps);
        Assert.Contains(4, status.CompletedSteps);
        Assert.Contains(5, status.CompletedSteps);
        Assert.Contains(6, status.CompletedSteps);
    }

    [Fact]
    public async Task PublicationReadiness_FailsIfMandatoryRequirementsAreMissing()
    {
        // Section 28 Step 12: Publication readiness evaluates actual business state.
        // Publication is not a cosmetic switch; checks actual readiness.
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Incomplete Salon",
            Slug = "incomplete-salon",
            BusinessType = "Salon",
            Description = "A brand new salon",
            OnboardingStep = 5,
            IsPublished = false,
            IsOnboardingCompleted = false
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // 1. Evaluate readiness before adding location, services, staff, working hours
        var readiness = await handler.Handle(new GetOnboardingReadinessQuery(), CancellationToken.None);

        Assert.False(readiness.IsReady);
        Assert.False(readiness.AllMandatorySatisfied);
        Assert.Contains(readiness.Requirements, r => r.Key == "primary_location" && !r.IsSatisfied);
        Assert.Contains(readiness.Requirements, r => r.Key == "active_service" && !r.IsSatisfied);
        Assert.Contains(readiness.Requirements, r => r.Key == "active_staff" && !r.IsSatisfied);
        Assert.Contains(readiness.Requirements, r => r.Key == "working_hours" && !r.IsSatisfied);

        // 2. Attempting to publish must be rejected by the server
        var publishResult = await handler.Handle(new PublishStorefrontCommand(), CancellationToken.None);

        Assert.False(publishResult.Success);
        Assert.False(publishResult.IsPublished);
        Assert.NotNull(publishResult.MissingRequirements);
        Assert.NotEmpty(publishResult.MissingRequirements);

        // Verify database state: Tenant is still unpublished!
        var savedTenant = await db.Tenants.FirstAsync(t => t.Id == tenantId);
        Assert.False(savedTenant.IsPublished);
        Assert.False(savedTenant.IsOnboardingCompleted);
    }

    [Fact]
    public async Task PublishStorefront_SucceedsWhenAllMandatoryRequirementsMet_AndDistinguishesVerification()
    {
        // Section 28 Step 13: Publishing succeeds when ready.
        // Distinguishes business publication from platform approval/verification.
        var (db, context) = CreateContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Ready Studio",
            Slug = "ready-studio",
            BusinessType = "Hair Salon",
            Description = "Full service salon",
            Address = "50 Park Ave",
            TimeZoneId = "Asia/Kolkata",
            Currency = "INR",
            HoldDurationMinutes = 5,
            BookingHorizonDays = 30,
            VerificationStatus = VerificationStatus.Pending, // Distinct platform verification state
            ModerationStatus = ModerationStatus.Pending,
            IsPublished = false,
            IsOnboardingCompleted = false
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Main",
            Address = "50 Park Ave",
            IsActive = true
        };
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Styling Session",
            Price = 500m,
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };
        var staff = new DomainStaff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Elena Ray",
            IsActive = true
        };
        var hours = new WorkingHours
        {
            TenantId = tenantId,
            StaffId = staff.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };

        db.Tenants.Add(tenant);
        db.Locations.Add(location);
        db.Services.Add(service);
        db.Staff.Add(staff);
        db.WorkingHours.Add(hours);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Readiness evaluation
        var readiness = await handler.Handle(new GetOnboardingReadinessQuery(), CancellationToken.None);
        Assert.True(readiness.IsReady);

        // Publish storefront
        var publishResult = await handler.Handle(new PublishStorefrontCommand(), CancellationToken.None);

        Assert.True(publishResult.Success);
        Assert.True(publishResult.IsPublished);
        Assert.Equal("/store/ready-studio", publishResult.StorefrontUrl);

        // Database verified
        var publishedTenant = await db.Tenants.FirstAsync(t => t.Id == tenantId);
        Assert.True(publishedTenant.IsPublished);
        Assert.True(publishedTenant.IsOnboardingCompleted);
        Assert.Equal(13, publishedTenant.OnboardingStep);

        // Platform verification remains distinct and untouched
        Assert.Equal(VerificationStatus.Pending, publishedTenant.VerificationStatus);
        Assert.Equal(ModerationStatus.Pending, publishedTenant.ModerationStatus);

        // Audit log created
        Assert.True(await db.AuditLogs.AnyAsync(a => a.TenantId == tenantId && a.Action == "Storefront.Published"));
    }
}
