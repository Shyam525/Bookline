using System.Text.Json;
using Bookline.Application.Common.Models;
using Bookline.Domain.Constants;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class OutboxAndReminderWorkerTests
{
    private sealed class TestClock(Instant current) : IClock
    {
        public Instant GetCurrentInstant() => current;
    }

    private static BooklineDbContext CreateDbContext(string dbName)
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task ProcessRemindersAsync_WhenBookingIn24hWindow_Schedules24hReminderInOutbox()
    {
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var nowInstant = Instant.FromUtc(2026, 10, 19, 10, 0, 0);
        var clock = new TestClock(nowInstant);

        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(services, NullLogger<OutboxAndReminderWorker>.Instance, clock);

        var tenant = new Tenant { Name = "Spa Elite", TimeZoneId = "America/New_York" };
        var customer = new Customer { FirstName = "John", LastName = "Smith", Email = "john@example.com" };
        var staff = new Staff { Name = "Emma", TimeZoneId = "America/New_York" };
        var service = new Service { Name = "Deep Massage", DurationMinutes = 60 };

        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        db.Staff.Add(staff);
        db.Services.Add(service);

        // Exactly 24 hours ahead
        var startUtc = nowInstant.Plus(Duration.FromHours(24)).ToDateTimeOffset();
        var endUtc = startUtc.AddHours(1);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, customer.Id, startUtc, endUtc);
        booking.Confirm();
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var scheduledCount = await worker.ProcessRemindersAsync(db);

        Assert.Equal(1, scheduledCount);

        var outboxMsg = await db.OutboxMessages.FirstOrDefaultAsync(m => m.EventType == NotificationEvents.ReminderDue);
        Assert.NotNull(outboxMsg);
        Assert.Contains("24h", outboxMsg.Content);
        Assert.Contains(booking.Id.ToString(), outboxMsg.Content);

        var reloadedBooking = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(reloadedBooking!.ReminderSentAtUtc);
    }

    [Fact]
    public async Task ProcessRemindersAsync_WhenBookingIn2hWindow_Schedules2hReminderInOutbox()
    {
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var nowInstant = Instant.FromUtc(2026, 10, 19, 10, 0, 0);
        var clock = new TestClock(nowInstant);

        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(services, NullLogger<OutboxAndReminderWorker>.Instance, clock);

        var tenant = new Tenant { Name = "Hair Salon", TimeZoneId = "Asia/Kolkata" };
        var customer = new Customer { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        var staff = new Staff { Name = "Ravi" };
        var service = new Service { Name = "Haircut", DurationMinutes = 30 };

        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        db.Staff.Add(staff);
        db.Services.Add(service);

        // Exactly 2 hours ahead
        var startUtc = nowInstant.Plus(Duration.FromHours(2)).ToDateTimeOffset();
        var endUtc = startUtc.AddMinutes(30);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, customer.Id, startUtc, endUtc);
        booking.Confirm();
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var scheduledCount = await worker.ProcessRemindersAsync(db);

        Assert.Equal(1, scheduledCount);

        var outboxMsg = await db.OutboxMessages.FirstOrDefaultAsync(m => m.EventType == NotificationEvents.ReminderDue);
        Assert.NotNull(outboxMsg);
        Assert.Contains("2h", outboxMsg.Content);
    }

    [Fact]
    public async Task ProcessRemindersAsync_WhenBookingOutsideWindows_DoesNotSchedule()
    {
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var nowInstant = Instant.FromUtc(2026, 10, 19, 10, 0, 0);
        var clock = new TestClock(nowInstant);

        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(services, NullLogger<OutboxAndReminderWorker>.Instance, clock);

        var tenant = new Tenant { Name = "Spa Elite", TimeZoneId = "UTC" };
        var customer = new Customer { FirstName = "Sam", LastName = "Doe", Email = "sam@example.com" };
        var staff = new Staff { Name = "Emma" };
        var service = new Service { Name = "Facial", DurationMinutes = 45 };

        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        db.Staff.Add(staff);
        db.Services.Add(service);

        // 6 hours ahead (outside 24h and 2h windows)
        var startUtc = nowInstant.Plus(Duration.FromHours(6)).ToDateTimeOffset();
        var endUtc = startUtc.AddMinutes(45);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, customer.Id, startUtc, endUtc);
        booking.Confirm();
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var scheduledCount = await worker.ProcessRemindersAsync(db);

        Assert.Equal(0, scheduledCount);
        Assert.Empty(db.OutboxMessages);
    }

    [Fact]
    public async Task ProcessRemindersAsync_IsIdempotent_DoesNotDuplicateReminders()
    {
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var nowInstant = Instant.FromUtc(2026, 10, 19, 10, 0, 0);
        var clock = new TestClock(nowInstant);

        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(services, NullLogger<OutboxAndReminderWorker>.Instance, clock);

        var tenant = new Tenant { Name = "Salon", TimeZoneId = "UTC" };
        var customer = new Customer { FirstName = "Sam", LastName = "Doe", Email = "sam@example.com" };
        var staff = new Staff { Name = "Emma" };
        var service = new Service { Name = "Styling", DurationMinutes = 45 };

        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        db.Staff.Add(staff);
        db.Services.Add(service);

        var startUtc = nowInstant.Plus(Duration.FromHours(24)).ToDateTimeOffset();
        var endUtc = startUtc.AddMinutes(45);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, customer.Id, startUtc, endUtc);
        booking.Confirm();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var firstRun = await worker.ProcessRemindersAsync(db);
        Assert.Equal(1, firstRun);

        // Second run must be idempotent
        var secondRun = await worker.ProcessRemindersAsync(db);
        Assert.Equal(0, secondRun);

        var totalReminders = await db.OutboxMessages.CountAsync(m => m.EventType == NotificationEvents.ReminderDue);
        Assert.Equal(1, totalReminders);
    }

    [Fact]
    public async Task ProcessOutboxBatchAsync_ProcessesPendingMessages_UpdatesStatusToProcessed()
    {
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(services, NullLogger<OutboxAndReminderWorker>.Instance);

        var tenantId = Guid.NewGuid();
        var msg = new OutboxMessage
        {
            TenantId = tenantId,
            EventType = NotificationEvents.AppointmentCreated,
            Content = JsonSerializer.Serialize(new
            {
                BookingReference = "BL-TEST1",
                CustomerEmail = "customer@example.com"
            }),
            Status = "Pending"
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var processedCount = await worker.ProcessOutboxBatchAsync(db, emailSender: null);

        Assert.Equal(1, processedCount);

        var reloaded = await db.OutboxMessages.FindAsync(msg.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Processed", reloaded.Status);
        Assert.NotNull(reloaded.ProcessedAtUtc);
    }
}
