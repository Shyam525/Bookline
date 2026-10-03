using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Models;
using Bookline.Application.Locations.Commands;
using Bookline.Application.Locations.DTOs;
using Bookline.Application.Locations.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class LocationCommandHandlerTests
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
    public async Task CreateLocation_ShouldSaveLocationWithTenantContext()
    {
        // Arrange
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new LocationHandlers(db, context);

        // Act
        var result = await handler.Handle(new CreateLocationCommand(new CreateLocationRequest(
            "Flagship Branch", "123 Main St", "+1 555 0000", "Asia/Kolkata", "USD"
        )), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Flagship Branch", result.Name);
        Assert.Equal(tenantId, result.TenantId);

        var dbLocation = await db.Locations.FirstOrDefaultAsync(l => l.Id == result.Id);
        Assert.NotNull(dbLocation);
        Assert.True(dbLocation.IsActive);
        Assert.False(dbLocation.IsArchived);
    }

    [Fact]
    public async Task UpdateAndArchiveLocation_ShouldUpdateFieldsAndSoftDelete()
    {
        // Arrange
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new LocationHandlers(db, context);
        var location = await handler.Handle(new CreateLocationCommand(new CreateLocationRequest(
            "Original Branch", "100 Street", "+1 111 2222", "Asia/Kolkata", "USD"
        )), CancellationToken.None);

        // Act 1: Update
        var updated = await handler.Handle(new UpdateLocationCommand(location.Id, new UpdateLocationRequest(
            "Updated Branch", "200 Street", "+1 333 4444", "America/New_York", "EUR", true
        )), CancellationToken.None);

        Assert.Equal("Updated Branch", updated.Name);
        Assert.Equal("EUR", updated.Currency);

        // Act 2: Archive
        var archived = await handler.Handle(new ArchiveLocationCommand(location.Id), CancellationToken.None);
        Assert.True(archived);

        var activeLocations = await handler.Handle(new GetLocationsQuery(IncludeArchived: false), CancellationToken.None);
        Assert.Empty(activeLocations);
    }
}
