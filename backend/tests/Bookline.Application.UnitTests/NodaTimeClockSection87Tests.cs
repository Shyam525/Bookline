using Bookline.Application.Analytics.DTOs;
using Bookline.Application.Analytics.Handlers;
using Bookline.Application.Availability.Commands;
using Bookline.Application.Availability.DTOs;
using Bookline.Application.Availability.Handlers;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Application.Staff.Commands;
using Bookline.Application.Staff.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.UnitTests;

/// <summary>
/// PART IX — SECTION 87: NODATIME AND CLOCK CORRECTNESS
/// Comprehensive unit tests verifying:
/// - Authoritative IClock abstraction & clock advancement
/// - Hold expiry upon clock advancement
/// - Minimum advance booking notice policy
/// - Cancellation cutoff enforcement
/// - Appointments around midnight
/// - Location timezone differing from server
/// - Daylight-saving transitions
/// - Missing or invalid timezone validation
/// - Timezone-aware reporting date boundaries
/// </summary>
public class NodaTimeClockSection87Tests
{
    public class TestClock : IClock
    {
        private Instant _current;

        public TestClock(Instant initial)
        {
            _current = initial;
        }

        public Instant GetCurrentInstant() => _current;

        public void Advance(Duration duration)
        {
            _current += duration;
        }

        public void Set(Instant instant)
        {
            _current = instant;
        }
    }

    private BooklineDbContext CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public void ClockAdvancement_InDeterministicTests_ControlsTimePredictably()
    {
        // Arrange
        var initial = Instant.FromUtc(2026, 10, 15, 10, 0);
        var clock = new TestClock(initial);

        // Act & Assert
        Assert.Equal(initial, clock.GetCurrentInstant());

        clock.Advance(Duration.FromMinutes(15));
        Assert.Equal(Instant.FromUtc(2026, 10, 15, 10, 15), clock.GetCurrentInstant());

        clock.Advance(Duration.FromHours(2));
        Assert.Equal(Instant.FromUtc(2026, 10, 15, 12, 15), clock.GetCurrentInstant());

        clock.Advance(Duration.FromDays(1));
        Assert.Equal(Instant.FromUtc(2026, 10, 16, 12, 15), clock.GetCurrentInstant());
    }

    [Fact]
    public async Task HoldExpiry_ClockAdvancement_ExpiresHoldAccurately()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var slotInstant = Instant.FromUtc(2026, 10, 15, 14, 0);

        var initial = Instant.FromUtc(2026, 10, 15, 10, 0);
        var clock = new TestClock(initial);
        var holdService = new SlotHoldService(redis: null, clock: clock);

        // Act 1: Acquire hold for 5 minutes
        var holdId = await holdService.AcquireHoldAsync(tenantId, staffId, slotInstant, Duration.FromMinutes(5));
        Assert.NotNull(holdId);

        // Assert 1: Hold is valid right after creation
        var isValidInitial = await holdService.ValidateHoldAsync(tenantId, staffId, slotInstant, holdId.Value);
        Assert.True(isValidInitial);

        // Act 2: Advance clock by 4 minutes (still within 5 min window)
        clock.Advance(Duration.FromMinutes(4));
        var isValidBeforeExpiry = await holdService.ValidateHoldAsync(tenantId, staffId, slotInstant, holdId.Value);
        Assert.True(isValidBeforeExpiry);

        // Act 3: Advance clock past 5 minutes (4m + 1m1s = 5m1s elapsed)
        clock.Advance(Duration.FromSeconds(61));
        var isValidAfterExpiry = await holdService.ValidateHoldAsync(tenantId, staffId, slotInstant, holdId.Value);
        Assert.False(isValidAfterExpiry);

        // Assert 4: Now that original hold expired, a new hold can be acquired for the same slot
        var newHoldId = await holdService.AcquireHoldAsync(tenantId, staffId, slotInstant, Duration.FromMinutes(5));
        Assert.NotNull(newHoldId);
        Assert.NotEqual(holdId.Value, newHoldId.Value);
    }

    [Fact]
    public async Task MinimumAdvanceBooking_RejectsShortNotice_AndAcceptsEligibleNotice()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var initialTime = Instant.FromUtc(2026, 10, 15, 10, 0);
        var clock = new TestClock(initialTime);

        using var db = CreateDbContext(tenantId);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            MinimumNoticeHours = 2,
            BookingHorizonDays = 30,
            HoldDurationMinutes = 5
        });
        db.Staff.Add(new DomainStaff { Id = staffId, TenantId = tenantId, Name = "Alice", TimeZoneId = "UTC", IsActive = true });
        db.Services.Add(new Service { Id = serviceId, TenantId = tenantId, Name = "Massage", DurationMinutes = 60, Price = 100, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
        db.Customers.Add(new Customer { Id = customerId, TenantId = tenantId, FirstName = "Bob", LastName = "Jones", Email = "bob@example.com" });
        await db.SaveChangesAsync();

        var holdService = new SlotHoldService(redis: null, clock: clock);
        var slotEngine = new SlotEngine();
        var tenantContext = new TestTenantContext(tenantId);
        var handler = new CreateBookingCommandHandler(db, tenantContext, holdService, slotEngine, clock: clock);

        // Act & Assert 1: Slot at 11:00 (only 1 hour notice, minimum is 2 hours) -> REJECTED
        var shortNoticeTime = initialTime + Duration.FromHours(1);
        var hold1 = await holdService.AcquireHoldAsync(tenantId, staffId, shortNoticeTime, Duration.FromMinutes(5));
        var command1 = new CreateBookingCommand(staffId, serviceId, customerId, shortNoticeTime.ToDateTimeOffset(), hold1!.Value);

        var ex1 = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command1, CancellationToken.None));
        Assert.Contains("MINIMUM_NOTICE_REQUIRED", ex1.Message);

        // Act & Assert 2: Slot at 13:00 (3 hours notice, >= 2 hours minimum) -> ACCEPTED
        var validNoticeTime = initialTime + Duration.FromHours(3);
        var hold2 = await holdService.AcquireHoldAsync(tenantId, staffId, validNoticeTime, Duration.FromMinutes(5));
        var command2 = new CreateBookingCommand(staffId, serviceId, customerId, validNoticeTime.ToDateTimeOffset(), hold2!.Value);

        var result2 = await handler.Handle(command2, CancellationToken.None);
        Assert.NotNull(result2);
        Assert.Equal(validNoticeTime.ToDateTimeOffset(), result2.StartUtc);
    }

    [Fact]
    public async Task BookingHorizon_RejectsBookingsBeyondConfiguredHorizon()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var initialTime = Instant.FromUtc(2026, 10, 15, 10, 0);
        var clock = new TestClock(initialTime);

        using var db = CreateDbContext(tenantId);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            MinimumNoticeHours = 1,
            BookingHorizonDays = 14, // 14-day horizon
            HoldDurationMinutes = 5
        });
        db.Staff.Add(new DomainStaff { Id = staffId, TenantId = tenantId, Name = "Alice", TimeZoneId = "UTC", IsActive = true });
        db.Services.Add(new Service { Id = serviceId, TenantId = tenantId, Name = "Haircut", DurationMinutes = 30, Price = 50, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
        db.Customers.Add(new Customer { Id = customerId, TenantId = tenantId, FirstName = "Bob", LastName = "Jones", Email = "bob@example.com" });
        await db.SaveChangesAsync();

        var holdService = new SlotHoldService(redis: null, clock: clock);
        var slotEngine = new SlotEngine();
        var tenantContext = new TestTenantContext(tenantId);
        var handler = new CreateBookingCommandHandler(db, tenantContext, holdService, slotEngine, clock: clock);

        // Act & Assert: Slot at 15 days ahead (> 14 days) -> REJECTED
        var tooFarTime = initialTime + Duration.FromDays(15);
        var hold = await holdService.AcquireHoldAsync(tenantId, staffId, tooFarTime, Duration.FromMinutes(5));
        var command = new CreateBookingCommand(staffId, serviceId, customerId, tooFarTime.ToDateTimeOffset(), hold!.Value);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("BOOKING_HORIZON_EXCEEDED", ex.Message);
    }

    [Fact]
    public async Task CancellationCutoff_EnforcesCutoffAgainstAuthoritativeClock()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var bookingStart = Instant.FromUtc(2026, 10, 15, 15, 0); // 3:00 PM UTC

        var initialTime = Instant.FromUtc(2026, 10, 15, 10, 0); // 10:00 AM UTC (5 hours prior)
        var clock = new TestClock(initialTime);

        using var db = CreateDbContext(tenantId);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            MinimumNoticeHours = 4 // Cutoff is 4 hours prior
        });

        var booking = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), bookingStart.ToDateTimeOffset(), (bookingStart + Duration.FromMinutes(60)).ToDateTimeOffset())
        {
            Id = bookingId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new CancelBookingCommandHandler(db, clock);

        // Act 1: Cancel at 10:00 AM (5 hours ahead > 4h cutoff) -> SUCCEEDS
        await handler.Handle(new CancelBookingCommand(bookingId, "Need to reschedule", "Customer"), CancellationToken.None);
        var updated = await db.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Cancelled, updated!.Status);

        // Act 2: Create another booking and advance clock past cutoff
        var booking2Id = Guid.NewGuid();
        var booking2 = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), bookingStart.ToDateTimeOffset(), (bookingStart + Duration.FromMinutes(60)).ToDateTimeOffset())
        {
            Id = booking2Id
        };
        db.Bookings.Add(booking2);
        await db.SaveChangesAsync();

        // Advance to 12:00 PM (3 hours ahead < 4h cutoff)
        clock.Set(Instant.FromUtc(2026, 10, 15, 12, 0));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CancelBookingCommand(booking2Id, "Too late", "Customer", EnforceCancellationPolicy: true), CancellationToken.None));
        Assert.Contains("CANCELLATION_CUTOFF_EXCEEDED", ex.Message);

        // Act 3: Staff/Provider override (EnforceCancellationPolicy: false) succeeds even within cutoff
        await handler.Handle(new CancelBookingCommand(booking2Id, "Manager exception", "Staff", EnforceCancellationPolicy: false), CancellationToken.None);
        var updated2 = await db.Bookings.FindAsync(booking2Id);
        Assert.Equal(BookingStatus.Cancelled, updated2!.Status);
    }

    [Fact]
    public void AppointmentAroundMidnight_SpansCalendarDaysCorrectly()
    {
        // Arrange: Working window 22:00 to 02:00 next day or late evening 23:00 to 01:00
        var zone = DateTimeZoneProviders.Tzdb["Asia/Kolkata"]; // UTC+5:30
        var localDay = new LocalDate(2026, 10, 15);

        // Appointment from 23:30 local to 00:30 local (next day)
        var startLocal = localDay.At(new LocalTime(23, 30));
        var startZoned = startLocal.InZoneStrictly(zone);
        var startInstant = startZoned.ToInstant();

        var duration = Duration.FromHours(1);
        var endInstant = startInstant + duration;
        var endZoned = endInstant.InZone(zone);

        // Assert: Start is on Oct 15 local, End is on Oct 16 local
        Assert.Equal(new LocalDate(2026, 10, 15), startZoned.Date);
        Assert.Equal(new LocalTime(23, 30), startZoned.TimeOfDay);

        Assert.Equal(new LocalDate(2026, 10, 16), endZoned.Date);
        Assert.Equal(new LocalTime(0, 30), endZoned.TimeOfDay);

        // In UTC: 23:30 IST (+5:30) is 18:00 UTC on Oct 15
        Assert.Equal(Instant.FromUtc(2026, 10, 15, 18, 0), startInstant);
        Assert.Equal(Instant.FromUtc(2026, 10, 15, 19, 0), endInstant);
    }

    [Fact]
    public void LocationTimezone_DifferingFromServer_AvoidsTimezoneShifting()
    {
        // Location is America/New_York (UTC-4 EDT in July)
        var zoneNy = DateTimeZoneProviders.Tzdb["America/New_York"];
        var localDate = new LocalDate(2026, 7, 15);

        // Working window 09:00 - 17:00 NY time
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);
        var engine = new SlotEngine();
        var now = Instant.FromUtc(2026, 7, 1, 0, 0);

        var slots = engine.Compute(
            service,
            schedule,
            Array.Empty<Interval>(),
            Array.Empty<Interval>(),
            localDate,
            zoneNy,
            now,
            Duration.FromMinutes(60),
            Duration.Zero);

        // Assert: 8 slots generated
        Assert.Equal(8, slots.Count);

        // First slot: 9:00 AM NY = 13:00 UTC
        var firstSlotZoned = slots[0].Start.InZone(zoneNy);
        Assert.Equal(new LocalDate(2026, 7, 15), firstSlotZoned.Date);
        Assert.Equal(new LocalTime(9, 0), firstSlotZoned.TimeOfDay);
        Assert.Equal(Instant.FromUtc(2026, 7, 15, 13, 0), slots[0].Start);

        // Last slot: 16:00 NY = 20:00 UTC
        var lastSlotZoned = slots[^1].Start.InZone(zoneNy);
        Assert.Equal(new LocalDate(2026, 7, 15), lastSlotZoned.Date);
        Assert.Equal(new LocalTime(16, 0), lastSlotZoned.TimeOfDay);
        Assert.Equal(Instant.FromUtc(2026, 7, 15, 20, 0), slots[^1].Start);
    }

    [Fact]
    public void DaylightSavingTransitions_SpringForwardAndFallBack_HandledCorrectly()
    {
        var zoneNy = DateTimeZoneProviders.Tzdb["America/New_York"];
        var engine = new SlotEngine();
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);

        // 1. Spring Forward (March 9, 2025): 2:00 AM becomes 3:00 AM
        var springDay = new LocalDate(2025, 3, 9);
        var springSchedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(1, 0), new LocalTime(5, 0)) });
        var springSlots = engine.Compute(service, springSchedule, Array.Empty<Interval>(), Array.Empty<Interval>(), springDay, zoneNy, Instant.FromUtc(2025, 3, 1, 0, 0), Duration.FromMinutes(60), Duration.Zero);

        // 1:00 AM - 5:00 AM with missing 2-3 AM yields exactly 3 hours (1-2 AM, 3-4 AM, 4-5 AM)
        Assert.Equal(3, springSlots.Count);

        // 2. Fall Back (November 2, 2025): 1:00 AM repeated (25-hour day)
        var fallDay = new LocalDate(2025, 11, 2);
        var fallSchedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var fallSlots = engine.Compute(service, fallSchedule, Array.Empty<Interval>(), Array.Empty<Interval>(), fallDay, zoneNy, Instant.FromUtc(2025, 11, 1, 0, 0), Duration.FromMinutes(60), Duration.Zero);

        // Standard daytime window after transition yields 8 regular slots
        Assert.Equal(8, fallSlots.Count);
    }

    [Fact]
    public async Task TimezoneValidation_RejectsInvalidIdentifiers_AndDefaultsGracefully()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        using var db = CreateDbContext(tenantId);
        db.Tenants.Add(new Tenant { Id = tenantId, TimeZoneId = "America/New_York", MinimumNoticeHours = 1 });
        db.Staff.Add(new DomainStaff { Id = staffId, TenantId = tenantId, Name = "Alice", TimeZoneId = "America/New_York", IsActive = true });
        db.Services.Add(new Service { Id = serviceId, TenantId = tenantId, Name = "Cut", DurationMinutes = 30, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
        await db.SaveChangesAsync();

        var slotEngine = new SlotEngine();
        var availabilityHandler = new AvailabilityHandlers(db, slotEngine);

        // Act & Assert 1: Invalid timezone "Mars/Phobos" -> throws ValidationException
        var queryInvalid = new GetAvailabilitySlotsQuery(serviceId, null, "2026-10-15", "Mars/Phobos");
        var ex1 = await Assert.ThrowsAsync<ValidationException>(() => availabilityHandler.Handle(queryInvalid, CancellationToken.None));
        Assert.Contains("INVALID_TIMEZONE", ex1.Message);

        // Act & Assert 2: Empty/default timezone -> falls back to tenant timezone ("America/New_York")
        var queryEmpty = new GetAvailabilitySlotsQuery(serviceId, null, "2026-10-15", "");
        var slots = await availabilityHandler.Handle(queryEmpty, CancellationToken.None);
        Assert.NotNull(slots); // Doesn't throw, succeeds with fallback

        // Act & Assert 3: Staff creation with invalid timezone -> throws ValidationException
        var tenantContext = new TestTenantContext(tenantId);
        var staffHandler = new StaffCommandHandler(db, tenantContext);
        var createStaffCmd = new CreateStaffCommand(new CreateStaffRequest("Bob", "bob@example.com", TimeZoneId: "Invalid/Zone_Name"));

        var ex2 = await Assert.ThrowsAsync<ValidationException>(() => staffHandler.Handle(createStaffCmd, CancellationToken.None));
        Assert.Contains("INVALID_TIMEZONE", ex2.Message);
    }

    [Fact]
    public async Task Reporting_DerivesLocalBoundaries_InTenantTimeZone()
    {
        // Tenant is in America/New_York (UTC-4 EDT in July)
        var tenantId = Guid.NewGuid();
        using var db = CreateDbContext(tenantId);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            TimeZoneId = "America/New_York"
        });

        // Booking 1: July 14, 23:30 NY time = July 15, 03:30 UTC
        var booking1Utc = Instant.FromUtc(2026, 7, 15, 3, 30).ToDateTimeOffset();
        var booking1 = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), booking1Utc, booking1Utc.AddMinutes(30));
        booking1.Confirm();
        db.Bookings.Add(booking1);

        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Amount = 100m,
            CreatedAtUtc = booking1Utc.UtcDateTime,
            Status = PaymentStatus.Completed,
            PaymentType = PaymentType.FullPayment
        });

        // Booking 2: July 15, 00:30 NY time = July 15, 04:30 UTC
        var booking2Utc = Instant.FromUtc(2026, 7, 15, 4, 30).ToDateTimeOffset();
        var booking2 = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), booking2Utc, booking2Utc.AddMinutes(30));
        booking2.Confirm();
        db.Bookings.Add(booking2);

        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Amount = 150m,
            CreatedAtUtc = booking2Utc.UtcDateTime,
            Status = PaymentStatus.Completed,
            PaymentType = PaymentType.FullPayment
        });

        await db.SaveChangesAsync();

        var tenantContext = new TestTenantContext(tenantId);
        var clock = new TestClock(Instant.FromUtc(2026, 7, 16, 12, 0));
        var handler = new AnalyticsHandlers(db, tenantContext, clock);

        // Query July 14 only (local date boundaries)
        var summaryJuly14 = await handler.Handle(new GetAnalyticsSummaryQuery(
            StartDate: new DateTime(2026, 7, 14),
            EndDate: new DateTime(2026, 7, 14)
        ), CancellationToken.None);

        // Assert: Only Booking 1 (July 14 23:30 NY) is included
        Assert.Equal(1, summaryJuly14.TotalBookings);
        Assert.Equal(100m, summaryJuly14.TotalRevenue);

        // Query July 15 only (local date boundaries)
        var summaryJuly15 = await handler.Handle(new GetAnalyticsSummaryQuery(
            StartDate: new DateTime(2026, 7, 15),
            EndDate: new DateTime(2026, 7, 15)
        ), CancellationToken.None);

        // Assert: Only Booking 2 (July 15 00:30 NY) is included
        Assert.Equal(1, summaryJuly15.TotalBookings);
        Assert.Equal(150m, summaryJuly15.TotalRevenue);
    }
}
