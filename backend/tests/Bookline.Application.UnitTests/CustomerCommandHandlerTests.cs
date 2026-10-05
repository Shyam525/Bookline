using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
using Bookline.Application.Customers.DTOs;
using Bookline.Application.Customers.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class CustomerCommandHandlerTests
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
    public async Task CreateCustomer_ShouldSaveCustomerWithTenantContext()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new CustomerCommandHandler(db, context);

        var result = await handler.Handle(new CreateCustomerCommand(new CreateCustomerRequest(
            "Samantha",
            "Reed",
            "samantha@example.com",
            "+1 555 9876",
            "Prefers afternoon appointments",
            AvatarUrl: ""
        )), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Samantha", result.FirstName);
        Assert.Equal("Reed", result.LastName);
        Assert.Equal("Samantha Reed", result.FullName);
        Assert.Equal(tenantId, result.TenantId);

        var dbCustomer = await db.Customers.FirstOrDefaultAsync(c => c.Id == result.Id);
        Assert.NotNull(dbCustomer);
        Assert.False(dbCustomer.IsArchived);
    }

    [Fact]
    public async Task GetCustomers_WithSearchQuery_ShouldFilterByNameOrEmail()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new CustomerCommandHandler(db, context);

        await handler.Handle(new CreateCustomerCommand(new CreateCustomerRequest("Alice", "Smith", "alice@example.com", "123")), CancellationToken.None);
        await handler.Handle(new CreateCustomerCommand(new CreateCustomerRequest("Bob", "Jones", "bob@example.com", "456")), CancellationToken.None);

        var searchResult = await handler.Handle(new GetCustomersQuery("Alice", IncludeArchived: false), CancellationToken.None);

        Assert.NotNull(searchResult);
        Assert.Single(searchResult.Items);
        Assert.Equal("Alice Smith", searchResult.Items[0].FullName);
    }

    [Fact]
    public async Task ArchiveCustomer_ShouldSoftDeleteCustomer()
    {
        var (db, context) = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.SetTenantId(tenantId);

        var handler = new CustomerCommandHandler(db, context);

        var customer = await handler.Handle(new CreateCustomerCommand(new CreateCustomerRequest("Charlie", "Brown", "charlie@example.com", "789")), CancellationToken.None);

        var archiveResult = await handler.Handle(new ArchiveCustomerCommand(customer.Id), CancellationToken.None);

        Assert.True(archiveResult);

        var dbCustomer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        Assert.NotNull(dbCustomer);
        Assert.True(dbCustomer.IsArchived);
    }
}
