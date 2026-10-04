using Bookline.Application.Availability.Commands;
using Bookline.Application.Availability.Handlers;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class AvailabilityQueryHandlerTests
{
    private (BooklineDbContext DbContext, TenantContext TenantContext) CreateDbContext()
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
    public async Task GetAvailabilitySlots_WithWorkingHours_ShouldComputeAvailableSlots()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Haircut & Styling",
            DurationMinutes = 30,
            BufferBeforeMinutes = 0,
            BufferAfterMinutes = 15,
            Price = 50.00m,
            IsActive = true,
            IsArchived = false
        };
        db.Services.Add(service);

        var staff = new Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Elena Vance",
            Email = "elena@example.com",
            IsActive = true,
            IsArchived = false
        };
        db.Staff.Add(staff);

        db.StaffServices.Add(new StaffService
        {
            StaffId = staff.Id,
            ServiceId = service.Id,
            TenantId = tenantId
        });

        // Add Monday Working Hours 09:00 to 17:00
        db.WorkingHours.Add(new WorkingHours
        {
            Id = Guid.NewGuid(),
            StaffId = staff.Id,
            TenantId = tenantId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        });

        await db.SaveChangesAsync();

        var slotEngine = new SlotEngine();
        var handler = new AvailabilityHandlers(db, slotEngine);

        // Future Monday date
        var targetMonday = "2028-10-02";

        var slots = await handler.Handle(new GetAvailabilitySlotsQuery(
            service.Id,
            staff.Id,
            targetMonday,
            "UTC"
        ), CancellationToken.None);

        Assert.NotNull(slots);
        Assert.NotEmpty(slots);
        Assert.All(slots, s => Assert.Equal(staff.Id, s.StaffId));
    }

    [Fact]
    public async Task GetStaffTimeOff_ShouldReturnActiveTimeOffs()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var staffId = Guid.NewGuid();
        db.TimeOffs.Add(new TimeOff
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            TenantId = tenantId,
            StartUtc = DateTimeOffset.UtcNow.AddDays(1),
            EndUtc = DateTimeOffset.UtcNow.AddDays(5),
            Reason = "Vacation"
        });
        await db.SaveChangesAsync();

        var slotEngine = new SlotEngine();
        var handler = new AvailabilityHandlers(db, slotEngine);

        var result = await handler.Handle(new GetStaffTimeOffQuery(staffId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Vacation", result[0].Reason);
    }
}
