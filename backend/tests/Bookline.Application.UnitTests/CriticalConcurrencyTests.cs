using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using StaffEntity = Bookline.Domain.Entities.Staff;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Specification Section 130: CRITICAL CONCURRENCY TEST
/// 50 users.
/// Same provider.
/// Same location.
/// Same staff.
/// Same service.
/// Same time.
/// EXPECTED:
/// exactly one successful booking.
/// 49 rejected.
/// Database has exactly one confirmed appointment.
/// </summary>
public class CriticalConcurrencyTests
{
    private BooklineDbContext CreateDbContext(string dbName)
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
    public async Task Section130_FiftyUsers_SameSlot_ExactlyOneSucceeds_FortyNineRejected_DbHasExactlyOne()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);

        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var slotTime = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10); // 10:00 AM UTC

        // Seed provider, location, staff, service
        var tenant = new Tenant { Id = tenantId, Name = "Aura Wellness", Slug = "aura-wellness", IsActive = true };
        var location = new Location { Id = locationId, TenantId = tenantId, Name = "Main Branch", City = "Ahmedabad", IsActive = true };
        var staff = new StaffEntity { Id = staffId, TenantId = tenantId, Name = "Dr. Priya Patel", IsActive = true };
        var service = new Service
        {
            Id = serviceId,
            TenantId = tenantId,
            Name = "Ayurvedic Restorative Facial",
            DurationMinutes = 60,
            Price = 75.00m,
            IsActive = true
        };

        db.Tenants.Add(tenant);
        db.Locations.Add(location);
        db.Staff.Add(staff);
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var holdService = new SlotHoldService(redis: null);
        var concurrency = 50;

        // Act: 50 distinct users attempt to hold and book the exact same slot concurrently
        var bookingTasks = Enumerable.Range(0, concurrency).Select(async userIndex =>
        {
            var userEmail = $"user{userIndex}@bookline.local";
            var userName = $"User {userIndex}";

            // 1. Attempt hold acquisition
            var holdId = await holdService.AcquireHoldAsync(
                tenantId,
                staffId,
                slotTime,
                TimeSpan.FromMinutes(5));

            if (!holdId.HasValue)
            {
                // Slot unavailable
                return (Success: false, BookingId: (Guid?)null, Error: "SLOT_UNAVAILABLE");
            }

            // 2. Confirmed booking submission
            try
            {
                using var userDb = CreateDbContext(dbName);
                var tenantCtx = new TenantContext();
                tenantCtx.SetTenantId(tenantId);

                var booking = new Booking(
                    tenantId,
                    staffId,
                    serviceId,
                    Guid.NewGuid(), // distinct customer
                    slotTime,
                    slotTime.AddMinutes(service.DurationMinutes))
                {
                    LocationId = locationId,
                    TotalPrice = service.Price,
                    CustomerNotes = $"Booked by {userName}"
                };
                booking.Confirm();

                userDb.Bookings.Add(booking);
                await userDb.SaveChangesAsync();

                return (Success: true, BookingId: (Guid?)booking.Id, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, BookingId: (Guid?)null, Error: ex.Message);
            }
        }).ToList();

        var outcomes = await Task.WhenAll(bookingTasks);

        // Assert: Exactly one successful booking, 49 rejected
        var successfulOutcomes = outcomes.Where(o => o.Success).ToList();
        var rejectedOutcomes = outcomes.Where(o => !o.Success).ToList();

        Assert.Single(successfulOutcomes);
        Assert.Equal(49, rejectedOutcomes.Count);
        Assert.All(rejectedOutcomes, r => Assert.Equal("SLOT_UNAVAILABLE", r.Error));

        // Assert: Database contains exactly one confirmed appointment for this slot & staff
        using var verificationDb = CreateDbContext(dbName);
        var persistedBookings = await verificationDb.Bookings
            .Where(b => b.TenantId == tenantId && b.StaffId == staffId && b.StartUtc == slotTime)
            .ToListAsync();

        Assert.Single(persistedBookings);
        Assert.Equal(successfulOutcomes[0].BookingId, persistedBookings[0].Id);
        Assert.Equal(BookingStatus.Confirmed, persistedBookings[0].Status);
    }
}
