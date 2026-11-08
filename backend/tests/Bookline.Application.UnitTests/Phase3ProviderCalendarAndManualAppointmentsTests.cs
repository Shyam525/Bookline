namespace Bookline.Application.UnitTests;

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
using Bookline.Application.Customers.Commands;
using Bookline.Application.Customers.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Domain.Exceptions;
using Bookline.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

public class Phase3ProviderCalendarAndManualAppointmentsTests
{
    private (BooklineDbContext Db, TenantContext TenantCtx, IAppointmentSchedulingService SchedulingService) CreateTestContext(Guid? tenantId = null)
    {
        var activeTenantId = tenantId ?? Guid.NewGuid();
        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(activeTenantId);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"Db_Phase3_{Guid.NewGuid()}")
            .Options;

        var db = new BooklineDbContext(options, tenantCtx);
        var schedulingService = new AppointmentSchedulingService(db);

        return (db, tenantCtx, schedulingService);
    }

    private async Task<(Tenant Tenant, DomainStaff Staff, Service Service)> SeedStandardBusinessAsync(BooklineDbContext db, Guid tenantId)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Aura Salon Bodakdev",
            Slug = "aura-salon-" + Guid.NewGuid().ToString("N")[..6],
            City = "Ahmedabad",
            Area = "Bodakdev",
            TimeZoneId = "Asia/Kolkata",
            Currency = "INR",
            IsActive = true
        };
        db.Tenants.Add(tenant);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Hair Care",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Name = "Signature Haircut & Wash",
            Slug = "signature-haircut",
            DurationMinutes = 45,
            Price = 450.00m,
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
            Name = "Vikram Mehta",
            Email = "vikram@aurasalon.local",
            Phone = "+91 98250 11111",
            TimeZoneId = "Asia/Kolkata",
            IsActive = true
        };
        db.Staff.Add(staff);

        // Working hours: Mon-Sun 09:00 to 18:00 (Split by break 13:00 to 14:00)
        for (int d = 0; d < 7; d++)
        {
            db.WorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staff.Id,
                DayOfWeek = (DayOfWeek)d,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(13, 0),
                Label = "Morning Shift"
            });
            db.WorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staff.Id,
                DayOfWeek = (DayOfWeek)d,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(18, 0),
                Label = "Afternoon Shift"
            });
        }

        db.StaffServices.Add(new StaffService
        {
            StaffId = staff.Id,
            ServiceId = service.Id,
            TenantId = tenantId
        });

        await db.SaveChangesAsync();
        return (tenant, staff, service);
    }

    [Fact]
    public async Task ManualAppointment_Creation_WithWalkInSource_SucceedsAndPreservesSnapshots()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        // Schedule for next Wednesday at 10:00 AM IST (04:30 AM UTC)
        var futureStartUtc = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(4).AddMinutes(30);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: futureStartUtc,
            Source: BookingSource.WalkIn,
            CustomerName: "Kunal Shah",
            CustomerPhone: "+91 98251 23456",
            CustomerEmail: "kunal@example.com",
            CustomerNotes: "Prefer scissor cut",
            InternalNotes: "VIP client, friend of owner"
        );

        var result = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal("WalkIn", result.Source);
        Assert.Equal(service.Name, result.ServiceName);
        Assert.Equal(service.Price, result.Price);
        Assert.Equal("VIP client, friend of owner", result.InternalNotes);
        Assert.Equal("Prefer scissor cut", result.CustomerNotes);

        // Verify entity persisted in DB
        var savedBooking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == result.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingSource.WalkIn, savedBooking.Source);
        Assert.Equal(service.Name, savedBooking.ServiceNameSnapshot);
        Assert.Equal(service.Price, savedBooking.PriceSnapshot);
        Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);

        // Verify customer record created
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == savedBooking.CustomerId);
        Assert.NotNull(customer);
        Assert.Equal("Kunal", customer.FirstName);
        Assert.Equal("Shah", customer.LastName);
        Assert.Equal("+91 98251 23456", customer.Phone);
    }

    [Theory]
    [InlineData(BookingSource.WalkIn)]
    [InlineData(BookingSource.Phone)]
    [InlineData(BookingSource.WhatsApp)]
    [InlineData(BookingSource.Instagram)]
    [InlineData(BookingSource.Manual)]
    public async Task ManualAppointment_ControlledSources_AllSupportedSourcesPersistCorrectly(BookingSource source)
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var futureStartUtc = DateTimeOffset.UtcNow.Date.AddDays(4).AddHours(5);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: futureStartUtc,
            Source: source,
            CustomerName: "Aarav Patel",
            CustomerPhone: "+91 98765 00000"
        );

        var result = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(source.ToString(), result.Source);

        var saved = await db.Bookings.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(source, saved.Source);
    }

    [Fact]
    public async Task ManualAppointment_PriceAndServiceSnapshot_NotOverwrittenByFutureEdits()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var futureStartUtc = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(5);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: futureStartUtc,
            Source: BookingSource.Phone,
            CustomerName: "Pooja Trivedi",
            CustomerPhone: "+91 98250 99999",
            PriceOverride: 400.00m // Special discounted price snapshot
        );

        var result = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        // Now modify service catalog price and name
        service.Price = 650.00m;
        service.Name = "Deluxe Haircut & Scalp Treatment";
        await db.SaveChangesAsync();

        // Verify that the appointment's snapshots remain unchanged
        var appointment = await db.Bookings.FindAsync(result.Id);
        Assert.NotNull(appointment);
        Assert.Equal(400.00m, appointment.PriceSnapshot);
        Assert.Equal("Signature Haircut & Wash", appointment.ServiceNameSnapshot);
    }

    [Fact]
    public async Task ManualAppointment_CustomerDeduplication_ReusesExistingCustomerByPhone()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var existingCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            FirstName = "Ananya",
            LastName = "Desai",
            Phone = "+91 99099 12345",
            Email = "ananya@example.com",
            TotalBookingsCount = 2,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Customers.Add(existingCustomer);
        await db.SaveChangesAsync();

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var futureStartUtc = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(5);

        // Receptionist enters a walk-in with the same phone number
        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: futureStartUtc,
            Source: BookingSource.WalkIn,
            CustomerName: "Ananya",
            CustomerPhone: "+91 99099 12345"
        );

        var result = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        Assert.Equal(existingCustomer.Id, result.CustomerId);

        // Verify count of customers in DB is still 1
        var customerCount = await db.Customers.CountAsync(c => c.TenantId == tenantCtx.TenantId);
        Assert.Equal(1, customerCount);

        var updatedCustomer = await db.Customers.FindAsync(existingCustomer.Id);
        Assert.Equal(3, updatedCustomer!.TotalBookingsCount);
    }

    [Fact]
    public async Task SchedulingRules_DoubleBooking_OverlappingAppointmentsRejected()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var slotStart = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(5); // 10:30 AM IST

        // Booking 1: 10:30 - 11:15 (45 mins)
        var req1 = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotStart,
            Source: BookingSource.Phone,
            CustomerName: "Client One"
        );
        await handler.Handle(new CreateManualBookingCommand(req1), CancellationToken.None);

        // Booking 2: Attempt overlapping slot 10:45 - 11:30
        var overlappingStart = slotStart.AddMinutes(15);
        var req2 = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: overlappingStart,
            Source: BookingSource.WalkIn,
            CustomerName: "Client Two"
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateManualBookingCommand(req2), CancellationToken.None));

        Assert.Contains("SCHEDULING_CONFLICT", ex.Message);
    }

    [Fact]
    public async Task SchedulingRules_InactiveStaff_Rejected()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        // Deactivate staff
        staff.IsActive = false;
        await db.SaveChangesAsync();

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var slotStart = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(5);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotStart,
            Source: BookingSource.WhatsApp,
            CustomerName: "New Client"
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None));

        Assert.Contains("STAFF_INACTIVE", ex.Message);
    }

    [Fact]
    public async Task SchedulingRules_StaffBreaks_SlotConflictingWithBreakRejected()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        // Staff shift has break 13:00 to 14:00 (IST).
        // 13:00 IST = 07:30 UTC.
        // Attempting appointment at 07:30 UTC (13:00 IST) should be rejected
        var breakStartUtc = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(7).AddMinutes(30);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: breakStartUtc,
            Source: BookingSource.Phone,
            CustomerName: "Break Slot Attempt"
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None));

        Assert.Contains("STAFF_UNAVAILABLE", ex.Message);
    }

    [Fact]
    public async Task SchedulingRules_PastAppointment_RejectedByDefault()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var pastStartUtc = DateTimeOffset.UtcNow.AddDays(-2);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: pastStartUtc,
            Source: BookingSource.WalkIn,
            CustomerName: "Past Walkin",
            AllowPast: false
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None));

        Assert.Contains("APPOINTMENT_IN_PAST", ex.Message);
    }

    [Fact]
    public async Task AppointmentLifecycle_StatusTransitions_EnforcesStateRules()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);
        var slotStart = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(5);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotStart,
            Source: BookingSource.WalkIn,
            CustomerName: "Lifecycle Test Customer"
        );

        var created = await handler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);
        var booking = await db.Bookings.FindAsync(created.Id);
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        // Confirmed -> CheckedIn
        booking.CheckIn();
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);

        // CheckedIn -> Completed
        booking.Complete();
        Assert.Equal(BookingStatus.Completed, booking.Status);

        // Terminal state: Cannot transition Completed -> Confirmed or Cancelled
        Assert.Throws<InvalidStatusTransitionException>(() => booking.Confirm());
        Assert.Throws<InvalidStatusTransitionException>(() => booking.Cancel("Test", "Provider"));
    }

    [Fact]
    public async Task CustomerHistory_Query_ReturnsAllAppointmentsForCustomer()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            FirstName = "Bhavin",
            LastName = "Joshi",
            Phone = "+91 98980 12345",
            Email = "bhavin@example.com",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Customers.Add(customer);

        // Add 2 bookings for Bhavin
        var b1 = Booking.CreateManual(
            tenantCtx.TenantId, staff.Id, service.Id, customer.Id,
            DateTimeOffset.UtcNow.Date.AddDays(-5).AddHours(5),
            DateTimeOffset.UtcNow.Date.AddDays(-5).AddHours(5).AddMinutes(45),
            BookingSource.WalkIn, service.Name, service.Price);

        var b2 = Booking.CreateManual(
            tenantCtx.TenantId, staff.Id, service.Id, customer.Id,
            DateTimeOffset.UtcNow.Date.AddDays(2).AddHours(5),
            DateTimeOffset.UtcNow.Date.AddDays(2).AddHours(5).AddMinutes(45),
            BookingSource.WhatsApp, service.Name, service.Price);

        db.Bookings.AddRange(b1, b2);
        await db.SaveChangesAsync();

        var customerHandler = new CustomerCommandHandler(db, tenantCtx);
        var history = await customerHandler.Handle(new GetCustomerAppointmentsQuery(customer.Id), CancellationToken.None);

        Assert.Equal(2, history.Count);
        Assert.Contains(history, x => x.Id == b1.Id && x.Source == "WalkIn");
        Assert.Contains(history, x => x.Id == b2.Id && x.Source == "WhatsApp");
    }

    [Fact]
    public async Task Calendar_Query_ReturnsEnrichedDetailsWithBusinessLocalContext()
    {
        var (db, tenantCtx, schedulingService) = CreateTestContext();
        var (tenant, staff, service) = await SeedStandardBusinessAsync(db, tenantCtx.TenantId);

        var slotStart = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(5);
        var manualHandler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var req = new CreateManualBookingRequest(
            StaffId: staff.Id,
            ServiceId: service.Id,
            StartUtc: slotStart,
            Source: BookingSource.Instagram,
            CustomerName: "Mitali Dave",
            CustomerPhone: "+91 97270 54321",
            CustomerNotes: "Allergic to sulfate shampoos",
            InternalNotes: "Always offer jasmine tea"
        );

        var created = await manualHandler.Handle(new CreateManualBookingCommand(req), CancellationToken.None);

        var calQueryHandler = new GetCalendarBookingsQueryHandler(db);
        var calendarResults = await calQueryHandler.Handle(new GetCalendarBookingsQuery(
            FromUtc: slotStart.AddHours(-1),
            ToUtc: slotStart.AddHours(2)
        ), CancellationToken.None);

        Assert.Single(calendarResults);
        var entry = calendarResults[0];

        Assert.Equal(created.Id, entry.Id);
        Assert.Equal("Instagram", entry.Source);
        Assert.Equal("Mitali Dave", entry.CustomerName);
        Assert.Equal("+91 97270 54321", entry.CustomerPhone);
        Assert.Equal("Allergic to sulfate shampoos", entry.CustomerNotes);
        Assert.Equal("Always offer jasmine tea", entry.InternalNotes);
        Assert.Equal("Asia/Kolkata", entry.TimeZoneId);
        Assert.Equal(service.Name, entry.ServiceName);
    }
}

