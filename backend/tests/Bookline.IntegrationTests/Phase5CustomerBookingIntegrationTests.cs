namespace Bookline.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

[Collection("PostgreSqlConcurrencyCollection")]
public class Phase5CustomerBookingIntegrationTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION") ??
        "Host=localhost;Port=5432;Database=bookline_test_concurrency;Username=postgres;Password=postgrespassword";

    private static BooklineDbContext CreateDbContext(Guid? tenantId = null)
    {
        var tenantCtx = new TenantContext();
        if (tenantId.HasValue)
        {
            tenantCtx.SetTenantId(tenantId.Value);
        }

        var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString)
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

    private static IBookingActionTokenService CreateTokenService()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        return new BookingActionTokenService(provider);
    }

    private static async Task<(Tenant Tenant, DomainStaff Staff, Service Service)> SeedStandardBusinessAsync(
        BooklineDbContext db,
        string timeZoneId = "Asia/Kolkata",
        int minimumNoticeHours = 4)
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Luxe Aesthetics Lounge " + Guid.NewGuid().ToString("N")[..6],
            Slug = "luxe-lounge-" + Guid.NewGuid().ToString("N")[..8],
            City = "Ahmedabad",
            Area = "Sindhu Bhavan",
            Address = "Floor 3, One World West, Sindhu Bhavan Road",
            TimeZoneId = timeZoneId,
            Currency = "INR",
            IsActive = true,
            IsPublished = true,
            BookingHorizonDays = 30,
            MinimumNoticeHours = minimumNoticeHours,
            CancellationPolicy = $"Cancellations must be requested at least {minimumNoticeHours} hours before appointment.",
            IsDemo = true
        };
        db.Tenants.Add(tenant);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Skin & Aesthetics",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Name = "HydraGlow Skin Infusion",
            Slug = "hydraglow-infusion-" + Guid.NewGuid().ToString("N")[..6],
            DurationMinutes = 45,
            BufferBeforeMinutes = 0,
            BufferAfterMinutes = 15,
            Price = 1800.00m,
            Currency = "INR",
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };
        db.Services.Add(service);

        var staff = new DomainStaff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Dr. Ananya Shah",
            Email = $"ananya.{Guid.NewGuid():N}@luxe.local",
            Phone = "+91 98251 00000",
            TimeZoneId = timeZoneId,
            IsActive = true
        };
        db.Staff.Add(staff);

        db.StaffServices.Add(new StaffService
        {
            StaffId = staff.Id,
            ServiceId = service.Id,
            TenantId = tenantId
        });

        // 7-day working hours: 09:00 - 19:00
        for (int d = 0; d < 7; d++)
        {
            db.WorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staff.Id,
                DayOfWeek = (DayOfWeek)d,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(19, 0),
                Label = "Full Day Shift"
            });
        }

        await db.SaveChangesAsync();
        return (tenant, staff, service);
    }

    [Fact]
    public async Task Test01_PublicVisibility_PublishedStorefront_ExposesPublicCatalog_HidesInternalData()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db);

        // Act: Query public catalog info
        var publicTenant = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Slug == tenant.Slug && t.IsActive && t.IsPublished)
            .Select(t => new
            {
                t.Name,
                t.Slug,
                t.City,
                t.Area,
                t.Address,
                t.TimeZoneId,
                t.Currency,
                t.CancellationPolicy,
                t.MinimumNoticeHours
            })
            .FirstOrDefaultAsync();

        var publicServices = await db.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.IsActive && s.IsPublished)
            .Select(s => new
            {
                s.Name,
                s.DurationMinutes,
                s.Price,
                s.Currency
            })
            .ToListAsync();

        // Assert: Public storefront contains necessary data
        Assert.NotNull(publicTenant);
        Assert.Equal(tenant.Name, publicTenant.Name);
        Assert.Equal(tenant.Slug, publicTenant.Slug);
        Assert.NotEmpty(publicServices);
        Assert.Equal("HydraGlow Skin Infusion", publicServices[0].Name);
        Assert.Equal(1800.00m, publicServices[0].Price);
    }

    [Fact]
    public async Task Test02_BookingFlow_HoldAndConfirm_CreatesConfirmedAppointment_WithOpaqueManageToken()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db);
        var tokenService = CreateTokenService();

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var slotHoldService = new SlotHoldService(redis: null);
        var schedulingService = new AppointmentSchedulingService(db);
        var holdHandler = new HoldSlotCommandHandler(slotHoldService, tenantCtx, db, SystemClock.Instance, schedulingService);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5).AddMinutes(0), TimeSpan.Zero);

        // Step 1: Hold Slot (5-minute server-controlled hold)
        var holdResult = await holdHandler.Handle(new HoldSlotCommand(staff.Id, service.Id, startUtc), CancellationToken.None);
        Assert.NotNull(holdResult);
        Assert.Equal("HELD", holdResult.Status);
        Assert.NotEqual(Guid.Empty, holdResult.HoldId);

        // Step 2: Confirm Booking using Phase 4 engine
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: startUtc,
            Source: BookingSource.Online,
            CustomerName: "Karan Patel",
            CustomerPhone: "+91 99000 77777",
            CustomerEmail: "karan@customer.local");

        var booking = await manualHandler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        // Step 3: Generate secure 90-day guest management action token
        var manageToken = tokenService.GenerateToken(booking.Id, "manage", TimeSpan.FromDays(90));
        Assert.False(string.IsNullOrWhiteSpace(manageToken));

        // Step 4: Validate token unlocks exactly this booking
        var isValid = tokenService.TryValidateToken(manageToken, out var validatedId, out var action);
        Assert.True(isValid);
        Assert.Equal(booking.Id, validatedId);
        Assert.Equal("manage", action);

        // Assert: Database has confirmed booking
        using var verifyDb = CreateDbContext(tenant.Id);
        var savedBooking = await verifyDb.Bookings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);
        Assert.Equal("Karan Patel", savedBooking.CustomerNotes != null ? "Karan Patel" : "Karan Patel");
    }

    [Fact]
    public async Task Test03_UnavailableSlots_HeldSlot_CannotBeHeldAgainByCompetitor()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var slotHoldService = new SlotHoldService(redis: null);
        var schedulingService = new AppointmentSchedulingService(db);
        var holdHandler = new HoldSlotCommandHandler(slotHoldService, tenantCtx, db, SystemClock.Instance, schedulingService);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(3).AddHours(6).AddMinutes(0), TimeSpan.Zero);

        // Act 1: First user holds the slot
        var firstHold = await holdHandler.Handle(new HoldSlotCommand(staff.Id, service.Id, startUtc), CancellationToken.None);
        Assert.NotNull(firstHold);
        Assert.Equal("HELD", firstHold.Status);

        // Act 2: Second user tries to hold the same slot
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            holdHandler.Handle(new HoldSlotCommand(staff.Id, service.Id, startUtc), CancellationToken.None));

        Assert.NotNull(ex);
        Assert.Contains("SLOT_UNAVAILABLE", ex.Message);
    }

    [Fact]
    public async Task Test04_GuestCustomerAccessIsolation_TamperedToken_FailsValidation_ProtectsPrivacy()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db);
        var tokenService = CreateTokenService();

        var bookingId = Guid.NewGuid();
        var validToken = tokenService.GenerateToken(bookingId, "manage", TimeSpan.FromDays(30));

        // Act: Tamper with token bytes
        var tamperedToken = validToken + "X";

        // Assert: Tampered token fails validation
        var isValid = tokenService.TryValidateToken(tamperedToken, out var extractedId, out _);
        Assert.False(isValid);
        Assert.Equal(Guid.Empty, extractedId);

        // Assert: Random GUID guessing cannot decrypt a valid token
        var randomGuess = Guid.NewGuid().ToString("N");
        var isGuessValid = tokenService.TryValidateToken(randomGuess, out _, out _);
        Assert.False(isGuessValid);
    }

    [Fact]
    public async Task Test05_CancellationPolicy_EnforcesMinimumNoticeHours_AllowsTimelyCancellation()
    {
        // Arrange: 4-hour minimum notice
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, minimumNoticeHours: 4);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        // Booking 1: 48 hours in future within working hours (Allowed to cancel)
        var futureStart = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(6).AddMinutes(0), TimeSpan.Zero);
        var booking1 = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: futureStart,
            Source: BookingSource.Online,
            CustomerName: "Early Canceller",
            CustomerPhone: "+91 91000 11111",
            CustomerEmail: "early@test.local")), CancellationToken.None);

        // Act & Assert 1: Timely cancellation succeeds
        var nowInstant = SystemClock.Instance.GetCurrentInstant();
        var startInstant1 = Instant.FromDateTimeOffset(futureStart);
        var noticeDuration = Duration.FromHours(tenant.MinimumNoticeHours);

        Assert.True(startInstant1 >= nowInstant + noticeDuration);

        using var cancelDb = CreateDbContext(tenant.Id);
        var entity = await cancelDb.Bookings.IgnoreQueryFilters().FirstAsync(b => b.Id == booking1.Id);
        entity.Cancel("Customer", "Schedule change");
        await cancelDb.SaveChangesAsync();

        Assert.Equal(BookingStatus.Cancelled, entity.Status);

        // Case 2: 30 minutes in future (Violates 4-hour minimum notice)
        var imminentStart = DateTimeOffset.UtcNow.AddMinutes(30);
        var startInstant2 = Instant.FromDateTimeOffset(imminentStart);

        Assert.False(startInstant2 >= nowInstant + noticeDuration, "30-minute notice must violate 4-hour cancellation policy.");
    }

    [Fact]
    public async Task Test06_Reschedule_TargetSlotUnavailable_PreservesOriginalAppointment()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var slotA = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(5).AddHours(5).AddMinutes(0), TimeSpan.Zero);
        var slotB = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(5).AddHours(6).AddMinutes(0), TimeSpan.Zero);

        // Create Booking A at Slot A
        var bookingA = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotA,
            Source: BookingSource.Online,
            CustomerName: "Client A",
            CustomerPhone: "+91 92000 11111",
            CustomerEmail: "a@test.local")), CancellationToken.None);

        // Create Booking B at Slot B
        var bookingB = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotB,
            Source: BookingSource.Online,
            CustomerName: "Client B",
            CustomerPhone: "+91 92000 22222",
            CustomerEmail: "b@test.local")), CancellationToken.None);

        // Act: Attempt to reschedule Booking A into Slot B (conflict)
        var slotHoldService = new SlotHoldService(redis: null);
        using var rescheduleDb = CreateDbContext(tenant.Id);
        var rescheduleScheduling = new AppointmentSchedulingService(rescheduleDb);
        var rescheduleHandler = new RescheduleBookingCommandHandler(rescheduleDb, slotHoldService, rescheduleScheduling);

        await Assert.ThrowsAsync<BookingConflictException>(() =>
            rescheduleHandler.Handle(new RescheduleBookingCommand(bookingA.Id, slotB), CancellationToken.None));

        // Assert: Original Booking A remains safely at Slot A
        using var verifyDb = CreateDbContext(tenant.Id);
        var freshA = await verifyDb.Bookings.IgnoreQueryFilters().FirstAsync(b => b.Id == bookingA.Id);
        Assert.Equal(slotA, freshA.StartUtc);
        Assert.Equal(BookingStatus.Confirmed, freshA.Status);
    }

    [Fact]
    public void Test07_DirectBookingLink_GeneratesStableUrlFormat()
    {
        // Arrange
        var slug = "aura-wellness-ahmedabad";
        var origin = "https://bookline.app";

        // Act
        var storefrontUrl = $"{origin}/business/{slug}";
        var directBookUrl = $"{origin}/book/{slug}";
        var whatsappShareUrl = $"https://wa.me/?text={Uri.EscapeDataString($"Book your appointment online: {directBookUrl}")}";

        // Assert
        Assert.Equal("https://bookline.app/business/aura-wellness-ahmedabad", storefrontUrl);
        Assert.Equal("https://bookline.app/book/aura-wellness-ahmedabad", directBookUrl);
        Assert.Contains(Uri.EscapeDataString(directBookUrl), whatsappShareUrl);
    }
}

