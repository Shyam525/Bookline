using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Models;
using FluentValidation;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Application.Services.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ServiceCommandHandlerTests
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
    public async Task CreateServiceCategory_ShouldSaveCategoryWithTenantContext()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new ServiceHandlers(db, context);

        var result = await handler.Handle(new CreateServiceCategoryCommand(new CreateServiceCategoryRequest(
            "Hair Styling", "Hair cutting and coloring", 1
        )), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Hair Styling", result.Name);
        Assert.Equal(tenantId, result.TenantId);

        var dbCategory = await db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == result.Id);
        Assert.NotNull(dbCategory);
    }

    [Fact]
    public async Task CreateService_ShouldSaveServiceAndCalculateTotalDuration()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new ServiceHandlers(db, context);

        var category = await handler.Handle(new CreateServiceCategoryCommand(new CreateServiceCategoryRequest("Hair Styling")), CancellationToken.None);

        var serviceResult = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            category.Id,
            "Haircut & Wash",
            "Full styling and wash",
            DurationMinutes: 45,
            BufferBeforeMinutes: 10,
            BufferAfterMinutes: 15,
            Price: 75.00m,
            Currency: "USD"
        )), CancellationToken.None);

        Assert.NotNull(serviceResult);
        Assert.Equal("Haircut & Wash", serviceResult.Name);
        Assert.Equal(45, serviceResult.DurationMinutes);
        Assert.Equal(70, serviceResult.TotalDurationMinutes); // 45 + 10 + 15
        Assert.Equal(75.00m, serviceResult.Price);
        Assert.Equal("Hair Styling", serviceResult.CategoryName);
    }

    [Fact]
    public async Task CreateService_WithInvalidDuration_ShouldThrowValidationException()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new ServiceHandlers(db, context);

        var category = await handler.Handle(new CreateServiceCategoryCommand(new CreateServiceCategoryRequest("Massage")), CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
                category.Id,
                "Express Massage",
                DurationMinutes: 0 // Invalid <= 0
            )), CancellationToken.None)
        );
    }

    [Fact]
    public async Task DuplicateService_ShouldCreateCopyWithCopiedName()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new ServiceHandlers(db, context);

        var category = await handler.Handle(new CreateServiceCategoryCommand(new CreateServiceCategoryRequest("Nails")), CancellationToken.None);

        var original = await handler.Handle(new CreateServiceCommand(new CreateServiceRequest(
            category.Id,
            "Gel Manicure",
            DurationMinutes: 60,
            Price: 50.00m
        )), CancellationToken.None);

        var duplicate = await handler.Handle(new DuplicateServiceCommand(original.Id), CancellationToken.None);

        Assert.NotNull(duplicate);
        Assert.NotEqual(original.Id, duplicate.Id);
        Assert.Equal("Gel Manicure (Copy)", duplicate.Name);
        Assert.Equal(60, duplicate.DurationMinutes);
        Assert.Equal(50.00m, duplicate.Price);
    }
}
