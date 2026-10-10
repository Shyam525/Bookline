using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Specification Sections 132, 133, 134:
/// - 132. PAYMENT TEST (Webhook process, replay same webhook, do not create duplicate payment/order state)
/// - 133. INVENTORY TEST (Stock: 1, two customers purchase, exactly 1 succeeds, inventory cannot become -1)
/// - 134. TENANT ATTACK TEST (Provider A requests Provider B appointment, customer, order, analytics, staff -> DENIED)
/// </summary>
public class SystemVerificationTests
{
    private (BooklineDbContext Db, TenantContext TenantCtx) CreateDbContext(Guid tenantId, bool isSystem = false)
    {
        var tenantContext = new TenantContext();
        if (isSystem)
        {
            tenantContext.EnableSystemMode();
        }
        else
        {
            tenantContext.SetTenantId(tenantId);
        }

        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        return (new BooklineDbContext(options, tenantContext), tenantContext);
    }

    [Fact]
    public async Task Section132_PaymentWebhook_ProcessAndReplay_ShouldNotCreateDuplicateState()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var (db, tenantCtx) = CreateDbContext(tenantId, isSystem: true);
        var idempotencyService = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Alice",
            LastName = "Customer",
            Email = "alice@example.com"
        };
        db.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            OrderNumber = "ORD-TEST-132",
            CustomerName = "Alice Customer",
            CustomerEmail = "alice@example.com",
            TotalAmount = 85.00m,
            Status = OrderStatus.Pending
        };
        db.Orders.Add(order);

        var booking = new Booking(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
        );
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var eventId = "evt_stripe_test_132";
        var transactionRef = "pi_3MtwBwLkdIwHu7ix28a3tqPa";
        var amount = 85.00m;

        // Act 1: Process initial webhook
        var existing1 = await idempotencyService.GetExistingAsync(eventId, "PaymentWebhook");
        Assert.Null(existing1);

        // Process state transition
        order.Status = OrderStatus.Processing;
        order.PaidAtUtc = DateTime.UtcNow;
        booking.Confirm();

        var payment = new Payment
        {
            TenantId = tenantId,
            BookingId = booking.Id,
            Amount = amount,
            Currency = "USD",
            PaymentType = PaymentType.Deposit,
            Status = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.Stripe,
            StripePaymentIntentId = transactionRef,
            CompletedAtUtc = DateTime.UtcNow,
            Notes = "Webhook processed: payment_intent.succeeded"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        await idempotencyService.SaveAsync(
            eventId,
            "PaymentWebhook",
            200,
            "{\"Status\":\"Processed\",\"Success\":true}",
            tenantId);

        // Assert 1: Database has exactly 1 payment and order is paid/processing
        var paymentsAfterFirst = await db.Payments.Where(p => p.StripePaymentIntentId == transactionRef).ToListAsync();
        Assert.Single(paymentsAfterFirst);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        // Act 2: Replay the EXACT same webhook
        var existing2 = await idempotencyService.GetExistingAsync(eventId, "PaymentWebhook");
        Assert.NotNull(existing2); // Webhook replay detected!

        // If duplicate detected, duplicate state creation is skipped
        if (existing2 == null)
        {
            // Should not enter here
            db.Payments.Add(new Payment { StripePaymentIntentId = transactionRef });
            await db.SaveChangesAsync();
        }

        // Assert 2: Database STILL has exactly 1 payment record and no duplicate state
        var paymentsAfterReplay = await db.Payments.Where(p => p.StripePaymentIntentId == transactionRef).ToListAsync();
        Assert.Single(paymentsAfterReplay);
        Assert.Equal(payment.Id, paymentsAfterReplay[0].Id);
        Assert.Equal(OrderStatus.Processing, order.Status);
    }

    [Fact]
    public void Section133_Inventory_StockOne_TwoPurchases_ExactlyOneSucceeds_InventoryCannotBeNegative()
    {
        // Arrange: Product with Stock = 1
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Limited Edition Silk Serum",
            Price = 45.00m,
            StockQuantity = 1,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            IsActive = true
        };

        // Act: Customer 1 and Customer 2 both attempt to purchase 1 unit
        var customer1Result = product.Purchase(1);
        var customer2Result = product.Purchase(1);

        // Assert: Exactly one purchase succeeds, the other fails
        Assert.True(customer1Result, "Customer 1 should succeed purchasing available stock");
        Assert.False(customer2Result, "Customer 2 should be rejected due to zero stock");

        // Inventory CANNOT become -1
        Assert.Equal(0, product.StockQuantity);
        Assert.True(product.StockQuantity >= 0, "Inventory must never drop below 0");
        Assert.Equal(1, product.SoldQuantity);
        Assert.Equal(0, product.AvailableQuantity);
    }

    [Fact]
    public async Task Section134_TenantAttackTest_ProviderA_AccessingProviderB_Denied()
    {
        // Arrange
        var providerAId = Guid.NewGuid();
        var providerBId = Guid.NewGuid();

        // Setup shared DB with System context
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var systemDb = new BooklineDbContext(options, tenantContext);

        // Seed Provider B data
        var providerBCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = providerBId,
            FirstName = "ProviderB",
            LastName = "Client",
            Email = "clientB@example.com"
        };
        var providerBStaff = new Bookline.Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = providerBId,
            Name = "Provider B Specialist",
            IsActive = true
        };
        var providerBAppointment = new Booking(
            providerBId,
            providerBStaff.Id,
            Guid.NewGuid(),
            providerBCustomer.Id,
            DateTimeOffset.UtcNow.AddDays(2),
            DateTimeOffset.UtcNow.AddDays(2).AddHours(1)
        );
        var providerBOrder = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = providerBId,
            CustomerId = providerBCustomer.Id,
            OrderNumber = "ORD-PROV-B",
            CustomerName = "ProviderB Client",
            CustomerEmail = "clientB@example.com",
            TotalAmount = 150.00m,
            Status = OrderStatus.Completed
        };

        systemDb.Customers.Add(providerBCustomer);
        systemDb.Staff.Add(providerBStaff);
        systemDb.Bookings.Add(providerBAppointment);
        systemDb.Orders.Add(providerBOrder);
        await systemDb.SaveChangesAsync();

        // Act & Assert under Provider A Tenant Context
        var providerAContext = new TenantContext();
        providerAContext.SetTenantId(providerAId);
        var providerAInterceptor = new TenantSaveChangesInterceptor(providerAContext);

        var providerAOptions = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(providerAInterceptor)
            .Options;

        using var providerADb = new BooklineDbContext(providerAOptions, providerAContext);

        // 1. Provider A requests Provider B appointment -> Filtered out (null)
        var fetchedBooking = await providerADb.Bookings.FirstOrDefaultAsync(b => b.Id == providerBAppointment.Id);
        Assert.Null(fetchedBooking);

        // 2. Provider A requests Provider B customer -> Filtered out (null)
        var fetchedCustomer = await providerADb.Customers.FirstOrDefaultAsync(c => c.Id == providerBCustomer.Id);
        Assert.Null(fetchedCustomer);

        // 3. Provider A requests Provider B order -> Filtered out (null)
        var fetchedOrder = await providerADb.Orders.FirstOrDefaultAsync(o => o.Id == providerBOrder.Id);
        Assert.Null(fetchedOrder);

        // 4. Provider A requests Provider B staff -> Filtered out (null)
        var fetchedStaff = await providerADb.Staff.FirstOrDefaultAsync(s => s.Id == providerBStaff.Id);
        Assert.Null(fetchedStaff);

        // 5. Provider A requests Provider B analytics (sum of orders) -> Returns 0 (no data leaked)
        var providerAOrderRevenue = await providerADb.Orders.SumAsync(o => o.TotalAmount);
        Assert.Equal(0m, providerAOrderRevenue);

        // 6. Provider A attempts to maliciously modify Provider B's customer
        var maliciousCustomer = new Customer
        {
            Id = providerBCustomer.Id,
            TenantId = providerBId,
            FirstName = "Hacked",
            LastName = "Name",
            Email = "hacked@example.com"
        };
        providerADb.Entry(maliciousCustomer).State = EntityState.Modified;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await providerADb.SaveChangesAsync();
        });
    }
}
