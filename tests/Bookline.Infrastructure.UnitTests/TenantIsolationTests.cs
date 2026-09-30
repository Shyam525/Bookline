using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class TenantIsolationTests
{
    private (BooklineDbContext DbContext, TenantContext TenantContext) CreateDbContext(string dbName)
    {
        var tenantContext = new TenantContext();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        return (context, tenantContext);
    }

    [Fact]
    public async Task QueryingEntities_WhenTenantAIsActive_ShouldNotSeeTenantBData()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed data for Tenant A & Tenant B using raw DbContext
        var (seedDb, seedContext) = CreateDbContext(dbName);
        seedContext.SetTenantId(tenantA);
        seedDb.Services.Add(new Service { Id = Guid.NewGuid(), Name = "Haircut A", TenantId = tenantA });
        await seedDb.SaveChangesAsync();

        var (seedDbB, seedContextB) = CreateDbContext(dbName);
        seedContextB.SetTenantId(tenantB);
        seedDbB.Services.Add(new Service { Id = Guid.NewGuid(), Name = "Coloring B", TenantId = tenantB });
        await seedDbB.SaveChangesAsync();

        // Act: Query as Tenant A
        var (queryDb, queryContext) = CreateDbContext(dbName);
        queryContext.SetTenantId(tenantA);
        var servicesA = await queryDb.Services.ToListAsync();

        // Assert
        Assert.Single(servicesA);
        Assert.Equal("Haircut A", servicesA[0].Name);
        Assert.Equal(tenantA, servicesA[0].TenantId);
    }

    [Fact]
    public async Task SaveChanges_WithoutActiveTenantContext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var (db, context) = CreateDbContext(dbName); // Unset tenant context

        db.Services.Add(new Service { Name = "Unassigned Service" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("Cannot save tenant entity without an active tenant context", ex.Message);
    }

    [Fact]
    public async Task SaveChanges_WithActiveTenantContext_ShouldAutomaticallyStampTenantId()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName);
        context.SetTenantId(tenantId);

        var service = new Service { Name = "Spa Massage" }; // TenantId not explicitly assigned
        db.Services.Add(service);

        // Act
        await db.SaveChangesAsync();

        // Assert
        Assert.Equal(tenantId, service.TenantId);
    }

    [Fact]
    public async Task SaveChanges_CrossTenantModification_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (seedDb, seedContext) = CreateDbContext(dbName);
        seedContext.SetTenantId(tenantA);
        var serviceA = new Service { Id = Guid.NewGuid(), Name = "Original A" };
        seedDb.Services.Add(serviceA);
        await seedDb.SaveChangesAsync();

        // Act: Attempt to modify Tenant A's service using Tenant B context
        var (mutateDb, mutateContext) = CreateDbContext(dbName);
        mutateContext.SetTenantId(tenantB);

        var existingService = await mutateDb.Services.IgnoreQueryFilters().FirstAsync(s => s.Id == serviceA.Id);
        existingService.Name = "Hijacked Name";

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mutateDb.SaveChangesAsync());
        Assert.Contains("Cross-tenant modification or deletion attempted", ex.Message);
    }
}
