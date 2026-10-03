using Bookline.Application.Common.Models;
using Bookline.Application.Staff.Commands;
using Bookline.Application.Staff.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class StaffCommandHandlerTests
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
    public async Task CreateStaff_ShouldSaveStaffWithTenantContext()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new StaffCommandHandler(db, context);

        var result = await handler.Handle(new CreateStaffCommand(new CreateStaffRequest(
            "Elena Vance",
            "elena@example.com",
            "+1 555 1234",
            "Senior Stylist",
            "Specializes in color and styling",
            TimeZoneId: "America/New_York"
        )), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Elena Vance", result.Name);
        Assert.Equal("elena@example.com", result.Email);
        Assert.Equal("Senior Stylist", result.Title);
        Assert.Equal(tenantId, result.TenantId);

        var dbStaff = await db.Staff.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(dbStaff);
        Assert.True(dbStaff.IsActive);
        Assert.False(dbStaff.IsArchived);
    }

    [Fact]
    public async Task AssignStaffServices_ShouldUpdateStaffServicesList()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new StaffCommandHandler(db, context);

        var staff = await handler.Handle(new CreateStaffCommand(new CreateStaffRequest(
            "Marcus Brody",
            "marcus@example.com",
            Title: "Barber"
        )), CancellationToken.None);

        var srv1 = Guid.NewGuid();
        var srv2 = Guid.NewGuid();

        var updated = await handler.Handle(new AssignStaffServicesCommand(staff.Id, new List<Guid> { srv1, srv2 }), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(2, updated.AssignedServiceIds.Count);
        Assert.Contains(srv1, updated.AssignedServiceIds);
        Assert.Contains(srv2, updated.AssignedServiceIds);
    }

    [Fact]
    public async Task ArchiveStaff_ShouldSoftDeleteStaff()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new StaffCommandHandler(db, context);

        var staff = await handler.Handle(new CreateStaffCommand(new CreateStaffRequest(
            "David Miller",
            "david@example.com"
        )), CancellationToken.None);

        var archived = await handler.Handle(new ArchiveStaffCommand(staff.Id), CancellationToken.None);

        Assert.True(archived);
        var dbStaff = await db.Staff.FirstOrDefaultAsync(s => s.Id == staff.Id);
        Assert.NotNull(dbStaff);
        Assert.True(dbStaff.IsArchived);
        Assert.False(dbStaff.IsActive);
    }
}
