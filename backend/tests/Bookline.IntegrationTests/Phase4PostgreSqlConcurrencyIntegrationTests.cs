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
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

[Collection("PostgreSqlConcurrencyCollection")]
public class Phase4PostgreSqlConcurrencyIntegrationTests
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

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new BooklineDbContext(options, tenantCtx);
    }

    private static async Task<(Tenant Tenant, DomainStaff Staff, Service Service)> SeedStandardPostgreSqlBusinessAsync(
        BooklineDbContext db,
        string timeZoneId = "Asia/Kolkata",
        int bufferBefore = 0,
        int bufferAfter = 0)
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Aura Real PG Salon " + Guid.NewGuid().ToString("N")[..6],
            Slug = "aura-pg-" + Guid.NewGuid().ToString("N")[..8],
            City = "Ahmedabad",
            Area = "Bodakdev",
            TimeZoneId = timeZoneId,
            Currency = "INR",
            IsActive = true,
            BookingHorizonDays = 60,
            MinimumNoticeHours = 1,
            IsDemo = true
        };
        db.Tenants.Add(tenant);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Salon Services",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Name = "Signature Styling",
            Slug = "signature-styling-" + Guid.NewGuid().ToString("N")[..6],
            DurationMinutes = 30,
            BufferBeforeMinutes = bufferBefore,
            BufferAfterMinutes = bufferAfter,
            Price = 500.00m,
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
            Name = "Rajesh Patel",
            Email = $"rajesh.{Guid.NewGuid():N}@aurasalon.local",
            Phone = "+91 98250 99999",
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

        // Add 7-day working hours: 09:00 - 18:00
        for (int d = 0; d < 7; d++)
        {
            db.WorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staff.Id,
                DayOfWeek = (DayOfWeek)d,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
                Label = "Regular Hours"
            });
        }

        await db.SaveChangesAsync();
        return (tenant, staff, service);
    }

    [Fact]
    public async Task Test01_TwoSimultaneousCompetingRequests_ExactlyOneSucceeds_OneGetsConflict()
    {
        // Arrange
        using var setupDb = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(setupDb);

        // Schedule slot tomorrow at 10:00 AM IST (04:30 AM UTC)
        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(4).AddMinutes(30), TimeSpan.Zero);

        // Act: 2 concurrent competing requests
        var results = new List<(bool Success, string? Error)>();
        var barrier = new TaskCompletionSource<bool>();

        var task1 = Task.Run(async () =>
        {
            await barrier.Task;
            using var db = CreateDbContext(tenant.Id);
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenant.Id);
            var schedulingService = new AppointmentSchedulingService(db);
            var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

            try
            {
                var req = new CreateManualBookingRequest(
                    StaffId: staff.Id,
                    ServiceId: service.Id,
                    StartUtc: startUtc,
                    Source: BookingSource.WalkIn,
                    CustomerName: "Client One",
                    CustomerPhone: "+91 99999 11111",
                    CustomerEmail: "client1@test.local");

                await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
                return (Success: true, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Error: ex.GetType().Name);
            }
        });

        var task2 = Task.Run(async () =>
        {
            await barrier.Task;
            using var db = CreateDbContext(tenant.Id);
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenant.Id);
            var schedulingService = new AppointmentSchedulingService(db);
            var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

            try
            {
                var req = new CreateManualBookingRequest(
                    StaffId: staff.Id,
                    ServiceId: service.Id,
                    StartUtc: startUtc,
                    Source: BookingSource.Phone,
                    CustomerName: "Client Two",
                    CustomerPhone: "+91 99999 22222",
                    CustomerEmail: "client2@test.local");

                await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
                return (Success: true, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Error: ex.GetType().Name);
            }
        });

        // Trigger simultaneous start
        barrier.SetResult(true);
        var outcomes = await Task.WhenAll(task1, task2);

        // Assert: Exactly 1 succeeded, exactly 1 got conflict
        var successCount = outcomes.Count(o => o.Success);
        var conflictCount = outcomes.Count(o => !o.Success && (o.Error == nameof(BookingConflictException) || o.Error == nameof(ValidationException)));

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        // Assert: Real PostgreSQL DB has exactly 1 booking
        using var verifyDb = CreateDbContext(tenant.Id);
        var confirmedBookings = await verifyDb.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.StaffId == staff.Id && b.StartUtc == startUtc && b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        Assert.Single(confirmedBookings);
    }

    [Fact]
    public async Task Test02_TenSimultaneousCompetingRequests_ExactlyOneSucceeds_NineGetConflict()
    {
        // Arrange
        using var setupDb = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(setupDb);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5).AddMinutes(0), TimeSpan.Zero);

        // Act: 10 concurrent requests
        var barrier = new TaskCompletionSource<bool>();
        var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(async () =>
        {
            await barrier.Task;
            using var db = CreateDbContext(tenant.Id);
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenant.Id);
            var schedulingService = new AppointmentSchedulingService(db);
            var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

            try
            {
                var req = new CreateManualBookingRequest(
                    StaffId: staff.Id,
                    ServiceId: service.Id,
                    StartUtc: startUtc,
                    Source: BookingSource.WalkIn,
                    CustomerName: $"Client {i}",
                    CustomerPhone: $"+91 99999 {10000 + i}",
                    CustomerEmail: $"client{i}@test.local");

                await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
                return (Success: true, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Error: ex.GetType().Name);
            }
        })).ToList();

        barrier.SetResult(true);
        var outcomes = await Task.WhenAll(tasks);

        // Assert: Exactly 1 succeeded, 9 failed with conflict
        var successCount = outcomes.Count(o => o.Success);
        var failureCount = outcomes.Count(o => !o.Success);

        Assert.Equal(1, successCount);
        Assert.Equal(9, failureCount);

        // Verify exactly 1 booking persisted in PostgreSQL
        using var verifyDb = CreateDbContext(tenant.Id);
        var bookingsInDb = await verifyDb.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.StaffId == staff.Id && b.StartUtc == startUtc)
            .ToListAsync();

        Assert.Single(bookingsInDb);
        Assert.Equal(BookingStatus.Confirmed, bookingsInDb[0].Status);
    }

    [Fact]
    public async Task Test03_FiftySimultaneousCompetingRequests_ExactlyOneSucceeds_FortyNineGetConflict()
    {
        // Arrange
        using var setupDb = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(setupDb);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(3).AddHours(5).AddMinutes(30), TimeSpan.Zero);

        // Act: 50 concurrent requests competing for the exact same staff time slot
        var barrier = new TaskCompletionSource<bool>();
        var tasks = Enumerable.Range(1, 50).Select(i => Task.Run(async () =>
        {
            await barrier.Task;
            using var db = CreateDbContext(tenant.Id);
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenant.Id);
            var schedulingService = new AppointmentSchedulingService(db);
            var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

            try
            {
                var req = new CreateManualBookingRequest(
                    StaffId: staff.Id,
                    ServiceId: service.Id,
                    StartUtc: startUtc,
                    Source: BookingSource.Online,
                    CustomerName: $"Racer {i}",
                    CustomerPhone: $"+91 88888 {20000 + i}",
                    CustomerEmail: $"racer{i}@test.local");

                await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
                return (Success: true, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Error: ex.GetType().Name);
            }
        })).ToList();

        barrier.SetResult(true);
        var outcomes = await Task.WhenAll(tasks);

        // Assert: Exactly 1 succeeds, exactly 49 get conflict
        var successCount = outcomes.Count(o => o.Success);
        var conflictCount = outcomes.Count(o => !o.Success);

        Assert.Equal(1, successCount);
        Assert.Equal(49, conflictCount);

        // Assert: Database level overlap protection preserved integrity
        using var verifyDb = CreateDbContext(tenant.Id);
        var bookings = await verifyDb.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.StaffId == staff.Id && b.StartUtc == startUtc)
            .ToListAsync();

        Assert.Single(bookings);
        Assert.Equal(BookingStatus.Confirmed, bookings[0].Status);
    }

    [Fact]
    public async Task Test04_ExpiredHolds_CleanupJob_FreesSlotsForAvailability()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(6).AddMinutes(0), TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        // Insert an expired booking hold
        var expiredHold = new BookingHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            StaffId = staff.Id,
            ServiceId = service.Id,
            StartUtc = startUtc,
            EndUtc = endUtc,
            HoldToken = "expired-token-123",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10), // EXPIRED 10 mins ago
            IsReleased = false,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-15)
        };
        db.BookingHolds.Add(expiredHold);
        await db.SaveChangesAsync();

        // Act: Run background cleanup job
        var cleanupJob = new ExpiredHoldsCleanupJob(db, NullLogger<ExpiredHoldsCleanupJob>.Instance);
        var cleanedCount = await cleanupJob.ExecuteAsync(CancellationToken.None);

        // Assert: Hold was cleaned up
        Assert.True(cleanedCount >= 1);

        var holdInDb = await db.BookingHolds.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == expiredHold.Id);
        Assert.Null(holdInDb);

        // Assert: Slot can now be held fresh
        var slotHoldService = new SlotHoldService(redis: null);
        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var holdHandler = new HoldSlotCommandHandler(slotHoldService, tenantCtx, db, SystemClock.Instance, schedulingService);

        var newHoldResult = await holdHandler.Handle(new HoldSlotCommand(staff.Id, service.Id, startUtc), CancellationToken.None);
        Assert.NotNull(newHoldResult);
        Assert.Equal("HELD", newHoldResult.Status);
    }

    [Fact]
    public async Task Test05_Idempotency_DuplicateRequest_ReturnsIdenticalResult_NoDuplicateBookingCreated()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var idempotencyService = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);
        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService, idempotencyService);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(6).AddMinutes(30), TimeSpan.Zero);
        var idempotencyKey = "manual-idemp-" + Guid.NewGuid().ToString("N");

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: startUtc,
            Source: BookingSource.WhatsApp,
            CustomerName: "Aarav Sharma",
            CustomerPhone: "+91 97777 55555",
            CustomerEmail: "aarav@test.local",
            IdempotencyKey: idempotencyKey);

        // Act 1: Initial call
        var firstResult = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
        Assert.NotNull(firstResult);
        Assert.Equal(BookingStatus.Confirmed, firstResult.Status);

        // Act 2: Retried duplicate call with same idempotency key
        var secondResult = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
        Assert.NotNull(secondResult);

        // Assert: Second call returned identical booking details
        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(firstResult.BookingReference, secondResult.BookingReference);

        // Assert: Exactly 1 booking entity exists in the real PostgreSQL database
        var bookingsCount = await db.Bookings
            .IgnoreQueryFilters()
            .CountAsync(b => b.StaffId == staff.Id && b.StartUtc == startUtc);

        Assert.Equal(1, bookingsCount);
    }

    [Fact]
    public async Task Test06_Reschedule_ConflictingTargetSlot_ThrowsConflict_PreservesOriginalBooking()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var timeSlotA = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(4).AddMinutes(0), TimeSpan.Zero);
        var timeSlotB = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(4).AddMinutes(30), TimeSpan.Zero);

        // Create Booking A at Slot A
        var bookingA = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: timeSlotA,
            Source: BookingSource.WalkIn,
            CustomerName: "Customer A",
            CustomerPhone: "+91 91111 00001",
            CustomerEmail: "a@test.local")), CancellationToken.None);

        // Create Booking B at Slot B
        var bookingB = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: timeSlotB,
            Source: BookingSource.WalkIn,
            CustomerName: "Customer B",
            CustomerPhone: "+91 91111 00002",
            CustomerEmail: "b@test.local")), CancellationToken.None);

        // Act: Attempt to reschedule Booking A to Slot B (which collides with Booking B)
        var slotHoldService = new SlotHoldService(redis: null);
        using var rescheduleDb = CreateDbContext(tenant.Id);
        var rescheduleScheduling = new AppointmentSchedulingService(rescheduleDb);
        var rescheduleHandler = new RescheduleBookingCommandHandler(rescheduleDb, slotHoldService, rescheduleScheduling);

        var rescheduleCmd = new RescheduleBookingCommand(
            BookingId: bookingA.Id,
            StartUtc: timeSlotB, // Collision with Booking B!
            HoldId: null);

        var ex = await Assert.ThrowsAsync<BookingConflictException>(() =>
            rescheduleHandler.Handle(rescheduleCmd, CancellationToken.None));

        Assert.NotNull(ex);

        // Assert: Original Booking A remains completely preserved at Slot A
        using var verifyDb = CreateDbContext(tenant.Id);
        var freshBookingA = await verifyDb.Bookings.IgnoreQueryFilters().FirstAsync(b => b.Id == bookingA.Id);

        Assert.Equal(timeSlotA, freshBookingA.StartUtc);
        Assert.Equal(timeSlotA.AddMinutes(service.DurationMinutes), freshBookingA.EndUtc);
        Assert.Equal(BookingStatus.Confirmed, freshBookingA.Status);

        // Assert: Booking B is untouched
        var freshBookingB = await verifyDb.Bookings.IgnoreQueryFilters().FirstAsync(b => b.Id == bookingB.Id);
        Assert.Equal(timeSlotB, freshBookingB.StartUtc);
        Assert.Equal(BookingStatus.Confirmed, freshBookingB.Status);
    }

    [Fact]
    public async Task Test07_SchedulingValidation_StaffTimeOff_RejectsBooking()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db);

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(5).AddHours(6).AddMinutes(0), TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        // Seed approved staff time off covering this window
        db.TimeOffs.Add(new TimeOff
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            StaffId = staff.Id,
            StartUtc = startUtc.AddMinutes(-30),
            EndUtc = endUtc.AddMinutes(30),
            Reason = "Medical appointment"
        });
        await db.SaveChangesAsync();

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: startUtc,
            Source: BookingSource.Phone,
            CustomerName: "Kavita Rao",
            CustomerPhone: "+91 93333 44444",
            CustomerEmail: "kavita@test.local");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            manualHandler.Handle(new CreateManualBookingCommand(req), CancellationToken.None));

        Assert.Contains("STAFF_TIME_OFF", ex.Message);
    }

    [Fact]
    public async Task Test08_SchedulingValidation_BusinessClosedDay_RejectsBooking()
    {
        // Arrange: Configure Sunday as Closed
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db);

        var weeklyHours = new List<BusinessDayHours>();
        for (int d = 0; d < 7; d++)
        {
            var dayOfWeek = (DayOfWeek)d;
            var isClosed = dayOfWeek == DayOfWeek.Sunday;

            weeklyHours.Add(new BusinessDayHours(
                DayOfWeek: dayOfWeek,
                IsOpen: !isClosed,
                OpenTime: isClosed ? null : new TimeOnly(9, 0),
                CloseTime: isClosed ? null : new TimeOnly(18, 0)));
        }

        tenant.BusinessHoursJson = System.Text.Json.JsonSerializer.Serialize(weeklyHours);
        await db.SaveChangesAsync();

        // Calculate upcoming Sunday date
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7;
        var sundayStartUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(daysUntilSunday).AddHours(5), TimeSpan.Zero);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: sundayStartUtc,
            Source: BookingSource.WalkIn,
            CustomerName: "Sunday Seeker",
            CustomerPhone: "+91 92222 11111",
            CustomerEmail: "sunday@test.local");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            manualHandler.Handle(new CreateManualBookingCommand(req), CancellationToken.None));

        Assert.Contains("OUTSIDE_BUSINESS_HOURS", ex.Message);
    }

    [Fact]
    public async Task Test09_SchedulingValidation_ServiceBuffers_BlocksAdjacentSlotOverlap()
    {
        // Arrange: Service with 15 min pre-buffer and 15 min post-buffer
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(
            db,
            bufferBefore: 15,
            bufferAfter: 15);

        var slot1 = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(8).AddMinutes(0), TimeSpan.Zero);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        // Book slot 1: 08:00 - 08:30 (with 15 min post-buffer -> staff busy until 08:45)
        var booking1 = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slot1,
            Source: BookingSource.WalkIn,
            CustomerName: "Buffer User 1",
            CustomerPhone: "+91 90000 11111",
            CustomerEmail: "buf1@test.local")), CancellationToken.None);

        Assert.NotNull(booking1);

        // Act: Query availability for this day via GetPublicAvailabilityQuery
        var slotEngine = new SlotEngine();
        var availabilityHandler = new GetPublicAvailabilityQueryHandler(db, slotEngine);

        var localDate = LocalDate.FromDateTime(slot1.Date);
        var query = new GetPublicAvailabilityQuery(
            StaffId: staff.Id,
            ServiceId: service.Id,
            Date: localDate,
            TimeZoneId: tenant.TimeZoneId,
            Slug: tenant.Slug);

        var availableSlots = await availabilityHandler.Handle(query, CancellationToken.None);

        // Assert: No available slot starts at 08:30 (inside the 15-minute post buffer)
        var blockedBufferSlot = slot1.AddMinutes(service.DurationMinutes);
        var containsBlockedSlot = availableSlots.Any(s => s.Start == Instant.FromDateTimeOffset(blockedBufferSlot));

        Assert.False(containsBlockedSlot, "The post-service buffer period must not be available for booking.");
    }

    [Fact]
    public async Task Test10_TimeZoneCorrectness_DstTransitionsAndLocalToInstantConversion()
    {
        // Arrange: New York timezone observes DST (EDT = UTC-4 in summer, EST = UTC-5 in winter)
        var tzId = "America/New_York";
        var zone = DateTimeZoneProviders.Tzdb[tzId];

        // Date in EDT (Summer: June 15, 2026) -> Offset is -04:00
        var summerDate = new LocalDate(2026, 6, 15);
        var localTime10Am = new LocalTime(10, 0);
        var zonedSummer = summerDate.At(localTime10Am).InZoneLeniently(zone);
        var summerInstant = zonedSummer.ToInstant();

        // 10:00 AM EDT -> 14:00 UTC (10 - (-4) = 14)
        var summerOffset = zone.GetUtcOffset(summerInstant);
        Assert.Equal(Offset.FromHours(-4), summerOffset);
        Assert.Equal(Instant.FromUtc(2026, 6, 15, 14, 0), summerInstant);

        // Date in EST (Winter: Dec 15, 2026) -> Offset is -05:00
        var winterDate = new LocalDate(2026, 12, 15);
        var zonedWinter = winterDate.At(localTime10Am).InZoneLeniently(zone);
        var winterInstant = zonedWinter.ToInstant();

        // 10:00 AM EST -> 15:00 UTC (10 - (-5) = 15)
        var winterOffset = zone.GetUtcOffset(winterInstant);
        Assert.Equal(Offset.FromHours(-5), winterOffset);
        Assert.Equal(Instant.FromUtc(2026, 12, 15, 15, 0), winterInstant);

        // Verify end-to-end against database business in America/New_York
        using var db = CreateDbContext();
        var (tenant, staff, service) = await SeedStandardPostgreSqlBusinessAsync(db, timeZoneId: tzId);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenant.Id);
        var schedulingService = new AppointmentSchedulingService(db);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        // Book on winter date at 10:00 AM New York time (15:00 UTC)
        var requestedUtc = winterInstant.ToDateTimeOffset();
        var result = await manualHandler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: requestedUtc,
            Source: BookingSource.Online,
            CustomerName: "John NYC",
            CustomerPhone: "+1 212 555 0199",
            CustomerEmail: "john@nyctest.local")), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(requestedUtc, result.StartUtc);
        Assert.Equal("America/New_York", result.TimeZoneId);
    }
}
