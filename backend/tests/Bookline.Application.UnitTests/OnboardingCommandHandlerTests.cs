using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Application.Tenants.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class OnboardingCommandHandlerTests
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
    public async Task SaveOnboardingStep_ShouldPersistProgressAndSeedServiceAndStaff()
    {
        // Arrange
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Onboarding Salon", Slug = "onboarding-salon", OnboardingStep = 1 };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act: Save Step 6 (First Service)
        var result = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
            6, "Onboarding Salon", "Salon", "123 Main St", "Asia/Kolkata", "USD",
            "Initial Haircut", 45, 50.00m, null, null, null, null, null, null
        )), CancellationToken.None);

        // Assert
        Assert.Equal(7, result.CurrentStep);
        var service = await db.Services.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        Assert.NotNull(service);
        Assert.Equal("Initial Haircut", service.Name);
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldMarkTenantCompletedAndActive()
    {
        // Arrange
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Unfinished Salon", Slug = "unfinished", IsOnboardingCompleted = false };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act
        var result = await handler.Handle(new CompleteOnboardingCommand(), CancellationToken.None);

        // Assert
        Assert.True(result.IsCompleted);
        Assert.Equal(10, result.CurrentStep);
    }
}
