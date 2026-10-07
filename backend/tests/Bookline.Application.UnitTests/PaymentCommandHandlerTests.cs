namespace Bookline.Application.UnitTests;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Payments.DTOs;
using Bookline.Application.Payments.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PaymentCommandHandlerTests
{
    private (BooklineDbContext DbContext, TestTenantContext TenantContext) CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
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
    public async Task RecordInStorePayment_ShouldCreatePaymentAndIncrementCustomerSpent()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var customer = new Customer
        {
            TenantId = tenantId,
            FirstName = "Marcus",
            LastName = "Vance",
            Email = "marcus@example.com",
            Phone = "+15550199",
            TotalSpentAmount = 100.00m
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handlers = new PaymentHandlers(db, context);

        var command = new RecordInStorePaymentCommand(
            BookingId: null,
            CustomerId: customer.Id,
            Amount: 50.00m,
            PaymentMethod: "Cash",
            Notes: "Haircut & Beard Trim"
        );

        var result = await handlers.Handle(command);

        Assert.NotNull(result);
        Assert.Equal(50.00m, result.Amount);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("InStorePOS", result.PaymentType);

        var updatedCustomer = await db.Customers.FirstAsync(c => c.Id == customer.Id);
        Assert.Equal(150.00m, updatedCustomer.TotalSpentAmount);
    }

    [Fact]
    public async Task GetPaymentSummary_ShouldCalculateFinancialTotals()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        db.Payments.AddRange(
            new Payment { TenantId = tenantId, Amount = 100.00m, PaymentType = PaymentType.Deposit, Status = PaymentStatus.Completed },
            new Payment { TenantId = tenantId, Amount = 200.00m, PaymentType = PaymentType.FullPayment, Status = PaymentStatus.Completed },
            new Payment { TenantId = tenantId, Amount = 50.00m, PaymentType = PaymentType.Refund, Status = PaymentStatus.Refunded }
        );
        await db.SaveChangesAsync();

        var handlers = new PaymentHandlers(db, context);

        var summary = await handlers.Handle(new GetPaymentSummaryQuery());

        Assert.NotNull(summary);
        Assert.Equal(300.00m, summary.TotalRevenue);
        Assert.Equal(100.00m, summary.TotalDeposits);
        Assert.Equal(50.00m, summary.TotalRefunds);
        Assert.Equal(3, summary.TotalTransactionsCount);
    }

    [Fact]
    public async Task ProcessRefund_ShouldUpdateOriginalStatusAndCreateRefundRecord()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var originalPayment = new Payment
        {
            TenantId = tenantId,
            Amount = 150.00m,
            Currency = "USD",
            PaymentType = PaymentType.FullPayment,
            Status = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.Stripe
        };
        db.Payments.Add(originalPayment);
        await db.SaveChangesAsync();

        var handlers = new PaymentHandlers(db, context);

        var refundResult = await handlers.Handle(new ProcessRefundCommand(
            PaymentId: originalPayment.Id,
            RefundAmount: 150.00m,
            Reason: "Customer requested cancellation within policy period"
        ));

        Assert.NotNull(refundResult);
        Assert.Equal("Refund", refundResult.PaymentType);
        Assert.Equal("Completed", refundResult.Status);

        var updatedOriginal = await db.Payments.FirstAsync(p => p.Id == originalPayment.Id);
        Assert.Equal(PaymentStatus.Refunded, updatedOriginal.Status);
    }
}
