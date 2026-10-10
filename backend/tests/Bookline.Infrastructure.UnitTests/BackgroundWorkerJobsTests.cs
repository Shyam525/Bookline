using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

/// <summary>
/// Unit tests verifying Section 122: Background Workers
/// - Expired holds
/// - Notifications delivery
/// - Invitation cleanup
/// - Inventory cleanup
/// - Report jobs
/// </summary>
public class BackgroundWorkerJobsTests
{
    private BooklineDbContext CreateDbContext(string dbName)
    {
        var tenantContext = new TenantContext();
        tenantContext.EnableSystemMode();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task ExpiredHoldsCleanupJob_ShouldReleaseExpiredHoldsAndKeepActiveHolds()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();

        var expiredHold = new BookingHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HoldToken = "HOLD-EXP-01",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10) // Expired
        };

        var activeHold = new BookingHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HoldToken = "HOLD-ACT-01",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10) // Active
        };

        db.BookingHolds.AddRange(expiredHold, activeHold);
        await db.SaveChangesAsync();

        var job = new ExpiredHoldsCleanupJob(db, NullLogger<ExpiredHoldsCleanupJob>.Instance);

        // Act
        var cleanedCount = await job.ExecuteAsync();

        // Assert
        Assert.Equal(1, cleanedCount);
        var remainingHolds = await db.BookingHolds.ToListAsync();
        Assert.Single(remainingHolds);
        Assert.Equal("HOLD-ACT-01", remainingHolds[0].HoldToken);
    }

    [Fact]
    public async Task NotificationDeliveryJob_ShouldProcessPendingNotificationsAndMarkSent()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();

        var pendingNotif = new NotificationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientEmail = "customer@example.com",
            Subject = "Appointment Confirmed",
            Body = "Your booking is confirmed.",
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Pending
        };

        db.NotificationLogs.Add(pendingNotif);
        await db.SaveChangesAsync();

        var job = new NotificationDeliveryJob(db, NullLogger<NotificationDeliveryJob>.Instance);

        // Act
        var processedCount = await job.ExecuteAsync();

        // Assert
        Assert.Equal(1, processedCount);
        var updatedNotif = await db.NotificationLogs.FindAsync(pendingNotif.Id);
        Assert.NotNull(updatedNotif);
        Assert.Equal(NotificationStatus.Sent, updatedNotif.Status);
        Assert.NotNull(updatedNotif.SentAtUtc);
    }

    [Fact]
    public async Task InvitationCleanupJob_ShouldRemoveStaleUnacceptedInvitations()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();

        var expiredInvite = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "stale@example.com",
            Role = "Staff",
            IsAccepted = false,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) // Stale
        };

        var activeInvite = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "active@example.com",
            Role = "Staff",
            IsAccepted = false,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(5) // Active
        };

        db.Invitations.AddRange(expiredInvite, activeInvite);
        await db.SaveChangesAsync();

        var job = new InvitationCleanupJob(db, NullLogger<InvitationCleanupJob>.Instance);

        // Act
        var cleanedCount = await job.ExecuteAsync();

        // Assert
        Assert.Equal(1, cleanedCount);
        var remainingInvites = await db.Invitations.ToListAsync();
        Assert.Single(remainingInvites);
        Assert.Equal("active@example.com", remainingInvites[0].Email);
    }

    [Fact]
    public async Task InventoryCleanupJob_ShouldRestoreProductStockFromAbandonedOrders()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Hair Gel",
            StockQuantity = 5,
            Price = 15.00m
        };
        db.Products.Add(product);

        var staleOrder = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = "ORD-ABANDONED",
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-45) // Abandoned (>30 min)
        };
        staleOrder.Items.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = staleOrder.Id,
            ProductId = product.Id,
            Quantity = 3,
            UnitPrice = 15.00m,
            TotalPrice = 45.00m
        });

        db.Orders.Add(staleOrder);
        await db.SaveChangesAsync();

        var job = new InventoryCleanupJob(db, NullLogger<InventoryCleanupJob>.Instance);

        // Act
        var cancelledCount = await job.ExecuteAsync();

        // Assert
        Assert.Equal(1, cancelledCount);

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(8, updatedProduct.StockQuantity); // 5 + 3 restored

        var updatedOrder = await db.Orders.FindAsync(staleOrder.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);
    }

    [Fact]
    public async Task ReportAggregationJob_ShouldAggregateActiveTenantsWithoutError()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Studio",
            Slug = "test-studio",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var job = new ReportAggregationJob(db, NullLogger<ReportAggregationJob>.Instance);

        // Act
        var processedTenants = await job.ExecuteAsync();

        // Assert
        Assert.Equal(1, processedTenants);
    }
}
