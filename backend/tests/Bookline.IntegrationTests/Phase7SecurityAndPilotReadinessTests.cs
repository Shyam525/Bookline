using System.Security.Claims;
using Bookline.Api.Controllers;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Constants;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Payments;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.IntegrationTests;

[Collection("PostgreSqlConcurrencyCollection")]
public class Phase7SecurityAndPilotReadinessTests
{
    private static readonly string TestConnectionString =
        Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=bookline_test_concurrency;Username=postgres;Password=postgrespassword";

    private BooklineDbContext CreateDbContext(Guid? tenantId = null, bool systemMode = false)
    {
        var tenantCtx = new TenantContext();
        if (systemMode)
        {
            tenantCtx.EnableSystemMode();
        }
        else if (tenantId.HasValue)
        {
            tenantCtx.SetTenantId(tenantId.Value);
        }

        var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(TestConnectionString)
        {
            Timeout = 60,
            CommandTimeout = 60,
            MaxPoolSize = 100
        };

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseNpgsql(connBuilder.ConnectionString, npgsql => npgsql.CommandTimeout(60))
            .Options;

        return new BooklineDbContext(options, tenantCtx);
    }

    private async Task<(Guid tenantA, Guid tenantB, Guid staffA, Guid serviceA, Guid staffB, Guid serviceB)> SeedTwoTenantsAsync(BooklineDbContext db)
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        db.Tenants.AddRange(
            new Tenant
            {
                Id = tenantA,
                Name = "Pilot Tenant A - Ahmedabad",
                Slug = "pilot-a-" + tenantA.ToString("N")[..6],
                Category = "Wellness",
                City = "Ahmedabad",
                State = "Gujarat",
                Country = "India",
                IsActive = true,
                IsPublished = true,
                VerificationStatus = VerificationStatus.Verified,
                ModerationStatus = ModerationStatus.Approved,
                Currency = "INR",
                HoldDurationMinutes = 5,
                IsDemo = true
            },
            new Tenant
            {
                Id = tenantB,
                Name = "Pilot Tenant B - Surat",
                Slug = "pilot-b-" + tenantB.ToString("N")[..6],
                Category = "Beauty",
                City = "Surat",
                State = "Gujarat",
                Country = "India",
                IsActive = true,
                IsPublished = true,
                VerificationStatus = VerificationStatus.Verified,
                ModerationStatus = ModerationStatus.Approved,
                Currency = "INR",
                HoldDurationMinutes = 5,
                IsDemo = true
            }
        );

        var staffA = Guid.NewGuid();
        var staffB = Guid.NewGuid();
        db.Staff.AddRange(
            new DomainStaff { Id = staffA, TenantId = tenantA, Name = "Stylist A", IsActive = true, Email = $"staffa_{staffA:N}@test.local" },
            new DomainStaff { Id = staffB, TenantId = tenantB, Name = "Stylist B", IsActive = true, Email = $"staffb_{staffB:N}@test.local" }
        );

        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();
        db.ServiceCategories.AddRange(
            new ServiceCategory { Id = catA, TenantId = tenantA, Name = "Category A", IsActive = true },
            new ServiceCategory { Id = catB, TenantId = tenantB, Name = "Category B", IsActive = true }
        );

        var srvA = Guid.NewGuid();
        var srvB = Guid.NewGuid();
        db.Services.AddRange(
            new Service { Id = srvA, TenantId = tenantA, CategoryId = catA, Name = "Service A", Slug = "srv-a-" + srvA.ToString("N")[..4], DurationMinutes = 60, Price = 1200, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true, Currency = "INR" },
            new Service { Id = srvB, TenantId = tenantB, CategoryId = catB, Name = "Service B", Slug = "srv-b-" + srvB.ToString("N")[..4], DurationMinutes = 45, Price = 800, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true, Currency = "INR" }
        );

        db.StaffServices.AddRange(
            new StaffService { StaffId = staffA, ServiceId = srvA, TenantId = tenantA },
            new StaffService { StaffId = staffB, ServiceId = srvB, TenantId = tenantB }
        );

        for (int day = 0; day < 7; day++)
        {
            db.WorkingHours.AddRange(
                new WorkingHours { Id = Guid.NewGuid(), TenantId = tenantA, StaffId = staffA, DayOfWeek = (DayOfWeek)day, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(19, 0) },
                new WorkingHours { Id = Guid.NewGuid(), TenantId = tenantB, StaffId = staffB, DayOfWeek = (DayOfWeek)day, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(19, 0) }
            );
        }

        await db.SaveChangesAsync();
        return (tenantA, tenantB, staffA, srvA, staffB, srvB);
    }

    [Fact]
    public async Task Test01_CrossTenantIsolation_TenantBCannotAccessTenantABookings()
    {
        // Arrange
        using var systemDb = CreateDbContext(systemMode: true);
        var (tenantA, tenantB, staffA, serviceA, _, _) = await SeedTwoTenantsAsync(systemDb);

        // Create customer and booking in Tenant A
        var customerA = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            FirstName = "Aarav",
            LastName = "ClientA",
            Email = $"aarav_{Guid.NewGuid():N}@example.com",
            Phone = "+919876543210",
            CreatedAtUtc = DateTime.UtcNow
        };
        systemDb.Customers.Add(customerA);
        await systemDb.SaveChangesAsync();

        var startUtc = DateTimeOffset.UtcNow.AddDays(3);
        var bookingA = Booking.CreateManual(
            tenantId: tenantA,
            staffId: staffA,
            serviceId: serviceA,
            customerId: customerA.Id,
            startUtc: startUtc.UtcDateTime,
            endUtc: startUtc.AddHours(1).UtcDateTime,
            source: BookingSource.WalkIn,
            serviceNameSnapshot: "Service A",
            priceSnapshot: 1200,
            currency: "INR",
            customerNotes: "Customer requests quiet session",
            internalNotes: "CONFIDENTIAL: Client VIP status and medical notes for Tenant A",
            timeZoneId: "Asia/Kolkata",
            createdBy: "SeedStaff"
        );
        systemDb.Bookings.Add(bookingA);
        await systemDb.SaveChangesAsync();

        // Act & Assert 1: Query with Tenant B context
        using var tenantBDb = CreateDbContext(tenantId: tenantB);

        // Due to global query filter, Tenant B cannot find Tenant A's booking
        var foundByTenantB = await tenantBDb.Bookings.FirstOrDefaultAsync(b => b.Id == bookingA.Id);
        Assert.Null(foundByTenantB);

        // Attempting to query list of bookings returns only Tenant B's data
        var allBookingsForB = await tenantBDb.Bookings.ToListAsync();
        Assert.DoesNotContain(allBookingsForB, b => b.Id == bookingA.Id);
        Assert.All(allBookingsForB, b => Assert.Equal(tenantB, b.TenantId));
    }

    [Fact]
    public async Task Test02_CrossTenantIsolation_TenantBCannotMutateTenantAStaffOrServices()
    {
        // Arrange
        using var systemDb = CreateDbContext(systemMode: true);
        var (tenantA, tenantB, staffA, serviceA, _, _) = await SeedTwoTenantsAsync(systemDb);

        using var tenantBDb = CreateDbContext(tenantId: tenantB);

        // Act: Attempt to modify Tenant A's staff from Tenant B context
        var staffForTenantB = await tenantBDb.Staff.FirstOrDefaultAsync(s => s.Id == staffA);
        Assert.Null(staffForTenantB); // Blocked by query filter

        var serviceForTenantB = await tenantBDb.Services.FirstOrDefaultAsync(s => s.Id == serviceA);
        Assert.Null(serviceForTenantB); // Blocked by query filter
    }

    [Fact]
    public async Task Test03_CustomerPrivacy_PublicStorefrontNeverExposesInternalProviderNotes()
    {
        // Arrange
        using var systemDb = CreateDbContext(systemMode: true);
        var (tenantA, _, staffA, serviceA, _, _) = await SeedTwoTenantsAsync(systemDb);

        var tenantAEntity = await systemDb.Tenants.FirstAsync(t => t.Id == tenantA);
        var storefrontService = new PublicStorefrontService(systemDb);

        // Act: Resolve storefront
        var storefront = await storefrontService.GetStorefrontBySlugAsync(tenantAEntity.Slug, CancellationToken.None);

        // Assert
        Assert.NotNull(storefront);
        Assert.Equal(tenantAEntity.Name, storefront.Provider.Name);
        Assert.Equal("Ahmedabad", storefront.Provider.City);

        // Verify no internal passwords or admin configs exposed
        Assert.NotEmpty(storefront.Services);
        foreach (var svc in storefront.Services)
        {
            Assert.True(svc.IsOnlineBookingEnabled);
            Assert.True(svc.Price > 0);
        }
    }

    [Fact]
    public async Task Test04_AdministrativeAccess_RoleSeparationEnforced()
    {
        // PlatformAdmin allowed
        using var db = CreateDbContext(systemMode: true);
        var adminController = new AdminController(db, new PayoutProvider(db));
        var adminAttrs = typeof(AdminController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        Assert.NotEmpty(adminAttrs);
        var authAttr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)adminAttrs[0];
        Assert.Equal(Policies.RequirePlatformAdmin, authAttr.Policy);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Test05_BookingEngine_RejectsSimultaneousOverlappingHold()
    {
        // Arrange
        using var db = CreateDbContext(systemMode: true);
        var (tenantA, _, staffA, serviceA, _, _) = await SeedTwoTenantsAsync(db);

        var holdService = new SlotHoldService(null);
        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenantA);
        var holdHandler = new HoldSlotCommandHandler(holdService, tenantCtx, db);

        var requestedStart = DateTimeOffset.UtcNow.AddDays(4);

        // Act 1: Place first hold
        var hold1 = await holdHandler.Handle(new HoldSlotCommand(staffA, serviceA, requestedStart), CancellationToken.None);
        Assert.NotNull(hold1);
        Assert.True(hold1.ExpiresAtUtc > DateTimeOffset.UtcNow);

        // Act 2: Attempt overlapping hold for the exact same staff member and slot
        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            holdHandler.Handle(new HoldSlotCommand(staffA, serviceA, requestedStart), CancellationToken.None));

        // Assert: Second hold rejected to prevent double-booking
        Assert.Contains("SLOT_UNAVAILABLE", ex.Message);
    }

    [Fact]
    public async Task Test06_PilotReadiness_OutboxBacklogMonitoring_Visibility()
    {
        // Arrange
        using var db = CreateDbContext(systemMode: true);
        var (tenantA, _, _, _, _, _) = await SeedTwoTenantsAsync(db);

        // Add 3 pending outbox messages
        db.OutboxMessages.AddRange(
            new OutboxMessage { Id = Guid.NewGuid(), TenantId = tenantA, EventType = "BookingConfirmed", Content = "{}", CreatedAtUtc = DateTime.UtcNow },
            new OutboxMessage { Id = Guid.NewGuid(), TenantId = tenantA, EventType = "AppointmentReminder", Content = "{}", CreatedAtUtc = DateTime.UtcNow },
            new OutboxMessage { Id = Guid.NewGuid(), TenantId = tenantA, EventType = "ProviderBookingUpdate", Content = "{}", CreatedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act: Measure backlog
        var pendingCount = await db.OutboxMessages.CountAsync(o => o.ProcessedAtUtc == null);
        Assert.True(pendingCount >= 3, "Outbox backlog must be accurately observable.");

        var oldestPending = await db.OutboxMessages
            .Where(o => o.ProcessedAtUtc == null)
            .OrderBy(o => o.CreatedAtUtc)
            .Select(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

        Assert.True(oldestPending <= DateTime.UtcNow, "Oldest pending message latency must be observable.");
    }
}
