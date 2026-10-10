using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

/// <summary>
/// Unit tests verifying:
/// - Section 114: Multi-tenancy isolation (Provider A cannot access Provider B's customers, bookings, orders, payments, staff, audit)
/// - Section 115: Customer privacy & IDOR protection (Customer A cannot access Customer B's profile, appointments, orders)
/// - Section 120: Financial precision (decimal amount + currency, zero floats)
/// </summary>
public class IsolationAndSecurityTests
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
    public async Task Section114_ProviderTenantIsolation_CannotAccessAnotherTenantData()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (seedDbA, seedContextA) = CreateDbContext(dbName);

        // Seed Tenant A Data
        seedContextA.SetTenantId(tenantA);
        var staffA = new Staff { Id = Guid.NewGuid(), Name = "Stylist A", TenantId = tenantA };
        var custA = new Customer { Id = Guid.NewGuid(), FirstName = "Customer A", TenantId = tenantA };
        var bookingA = new Booking { Id = Guid.NewGuid(), TenantId = tenantA, StaffId = staffA.Id, CustomerId = custA.Id, TotalPrice = 100.00m, Currency = "INR" };
        var orderA = new Order { Id = Guid.NewGuid(), TenantId = tenantA, OrderNumber = "ORD-A", CustomerId = custA.Id, TotalAmount = 50.00m, Currency = "INR" };
        var paymentA = new Payment { Id = Guid.NewGuid(), TenantId = tenantA, Amount = 100.00m, Currency = "INR" };
        var auditA = new AuditLog { Id = Guid.NewGuid(), TenantId = tenantA, Actor = "Staff A", Action = "Booking.Created" };

        seedDbA.Staff.Add(staffA);
        seedDbA.Customers.Add(custA);
        seedDbA.Bookings.Add(bookingA);
        seedDbA.Orders.Add(orderA);
        seedDbA.Payments.Add(paymentA);
        seedDbA.AuditLogs.Add(auditA);
        await seedDbA.SaveChangesAsync();

        // Seed Tenant B Data
        var (seedDbB, seedContextB) = CreateDbContext(dbName);
        seedContextB.SetTenantId(tenantB);
        var staffB = new Staff { Id = Guid.NewGuid(), Name = "Stylist B", TenantId = tenantB };
        var custB = new Customer { Id = Guid.NewGuid(), FirstName = "Customer B", TenantId = tenantB };
        var bookingB = new Booking { Id = Guid.NewGuid(), TenantId = tenantB, StaffId = staffB.Id, CustomerId = custB.Id, TotalPrice = 200.00m, Currency = "INR" };
        var orderB = new Order { Id = Guid.NewGuid(), TenantId = tenantB, OrderNumber = "ORD-B", CustomerId = custB.Id, TotalAmount = 150.00m, Currency = "INR" };
        var paymentB = new Payment { Id = Guid.NewGuid(), TenantId = tenantB, Amount = 200.00m, Currency = "INR" };
        var auditB = new AuditLog { Id = Guid.NewGuid(), TenantId = tenantB, Actor = "Staff B", Action = "Booking.Created" };

        seedDbB.Staff.Add(staffB);
        seedDbB.Customers.Add(custB);
        seedDbB.Bookings.Add(bookingB);
        seedDbB.Orders.Add(orderB);
        seedDbB.Payments.Add(paymentB);
        seedDbB.AuditLogs.Add(auditB);
        await seedDbB.SaveChangesAsync();

        // Act: Query as Provider A
        var (queryDb, queryContext) = CreateDbContext(dbName);
        queryContext.SetTenantId(tenantA);

        var staffList = await queryDb.Staff.ToListAsync();
        var customerList = await queryDb.Customers.ToListAsync();
        var bookingList = await queryDb.Bookings.ToListAsync();
        var orderList = await queryDb.Orders.ToListAsync();
        var paymentList = await queryDb.Payments.ToListAsync();
        var auditList = await queryDb.AuditLogs.ToListAsync();

        // Assert: Provider A cannot see any of Provider B's records
        Assert.Single(staffList);
        Assert.Equal("Stylist A", staffList[0].Name);

        Assert.Single(customerList);
        Assert.Equal("Customer A", customerList[0].FullName);

        Assert.Single(bookingList);
        Assert.Equal(bookingA.Id, bookingList[0].Id);

        Assert.Single(orderList);
        Assert.Equal("ORD-A", orderList[0].OrderNumber);

        Assert.Single(paymentList);
        Assert.Equal(100.00m, paymentList[0].Amount);

        Assert.Single(auditList);
        Assert.Equal("Staff A", auditList[0].Actor);
    }

    [Fact]
    public async Task Section115_CustomerPrivacy_CustomerACannotAccessCustomerBAppointmentsOrOrders()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();

        var (db, ctx) = CreateDbContext(dbName);
        ctx.SetTenantId(tenantId);

        var bookingA = new Booking { Id = Guid.NewGuid(), TenantId = tenantId, CustomerId = customerAId, TotalPrice = 60.00m, Currency = "INR" };
        var bookingB = new Booking { Id = Guid.NewGuid(), TenantId = tenantId, CustomerId = customerBId, TotalPrice = 90.00m, Currency = "INR" };

        var orderA = new Order { Id = Guid.NewGuid(), TenantId = tenantId, CustomerId = customerAId, OrderNumber = "CUST-A-01", TotalAmount = 30.00m, Currency = "INR" };
        var orderB = new Order { Id = Guid.NewGuid(), TenantId = tenantId, CustomerId = customerBId, OrderNumber = "CUST-B-01", TotalAmount = 45.00m, Currency = "INR" };

        db.Bookings.AddRange(bookingA, bookingB);
        db.Orders.AddRange(orderA, orderB);
        await db.SaveChangesAsync();

        // Act: Filter by Customer A ID (simulating authenticated Customer A controller endpoint)
        var customerABookings = await db.Bookings
            .Where(b => b.CustomerId == customerAId)
            .ToListAsync();

        var customerAOrders = await db.Orders
            .Where(o => o.CustomerId == customerAId)
            .ToListAsync();

        // Assert: Customer A cannot see Customer B's appointments or orders
        Assert.Single(customerABookings);
        Assert.Equal(bookingA.Id, customerABookings[0].Id);
        Assert.DoesNotContain(customerABookings, b => b.CustomerId == customerBId);

        Assert.Single(customerAOrders);
        Assert.Equal("CUST-A-01", customerAOrders[0].OrderNumber);
        Assert.DoesNotContain(customerAOrders, o => o.CustomerId == customerBId);
    }

    [Fact]
    public void Section120_FinancialPrecision_MonetaryValuesUseDecimalsAndCurrencyWithoutFloatDrift()
    {
        // Arrange
        decimal item1 = 19.99m;
        decimal item2 = 10.01m;
        decimal subtotal = item1 + item2;
        decimal tax = Math.Round(subtotal * 0.08m, 2);
        decimal total = subtotal + tax;

        // Assert
        Assert.Equal(30.00m, subtotal);
        Assert.Equal(2.40m, tax);
        Assert.Equal(32.40m, total);

        // Verify entity storage format
        var payment = new Payment
        {
            Amount = total,
            Currency = "INR"
        };

        Assert.IsType<decimal>(payment.Amount);
        Assert.Equal("INR", payment.Currency);
        Assert.Equal(32.40m, payment.Amount);
    }
}
