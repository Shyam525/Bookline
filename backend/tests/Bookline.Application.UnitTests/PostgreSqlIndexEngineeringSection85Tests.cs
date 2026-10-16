namespace Bookline.Application.UnitTests;

using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PostgreSqlIndexEngineeringSection85Tests
{
    private BooklineDbContext CreateInMemoryContext(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"Bookline_IndexTest_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
        {
            tenantContext.SetTenantId(tenantId.Value);
        }
        else
        {
            tenantContext.EnableSystemMode();
        }

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public void DbContext_Contains_All_Required_Section85_Booking_Indexes()
    {
        using var context = CreateInMemoryContext();
        var entityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        // 1. Staff schedule lookup & overlap query
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "StaffId", "StartUtc", "EndUtc", "Status" }));

        // 2. Provider agenda by location and date range
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "LocationId", "StartUtc", "EndUtc" }));

        // 3. Resource conflict detection
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "ResourceId", "StartUtc", "EndUtc" }));

        // 4. Customer appointment history with chronological sorting
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "CustomerId", "StartUtc", "Status" }));
    }

    [Fact]
    public void DbContext_Contains_All_Required_Section85_Order_Indexes()
    {
        using var context = CreateInMemoryContext();
        var entityType = context.Model.FindEntityType(typeof(Order));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        // 1. Provider orders by state with chronological ordering
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "Status", "CreatedAtUtc" }));

        // 2. Provider orders overall chronologically
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "CreatedAtUtc" }));

        // 3. Customer order history
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "CustomerId", "CreatedAtUtc" }));
    }

    [Fact]
    public void DbContext_Contains_All_Required_Section85_Review_Indexes()
    {
        using var context = CreateInMemoryContext();
        var entityType = context.Model.FindEntityType(typeof(Review));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        // 1. Provider approved reviews sorted newest
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "ModerationStatus", "CreatedAtUtc" }));

        // 2. Provider approved reviews sorted by rating
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "ModerationStatus", "Rating", "CreatedAtUtc" }));

        // 3. Customer review history
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "CustomerId", "CreatedAtUtc" }));
    }

    [Fact]
    public void DbContext_Contains_All_Required_Section85_Payment_Indexes()
    {
        using var context = CreateInMemoryContext();
        var entityType = context.Model.FindEntityType(typeof(Payment));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        // 1. Stripe payment intent webhook lookup
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "StripePaymentIntentId" }));

        // 2. Stripe checkout session webhook lookup
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "StripeCheckoutSessionId" }));

        // 3. Tenant payment ledger chronologically
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "Status", "CreatedAtUtc" }));

        // 4. Booking payment lookup
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "BookingId" }));

        // 5. Customer payment history
        Assert.Contains(indexes, idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "CustomerId", "CreatedAtUtc" }));
    }

    [Fact]
    public void DbContext_Contains_Outbox_And_Notification_HighThroughput_Indexes()
    {
        using var context = CreateInMemoryContext();
        
        // Outbox Messages
        var outboxType = context.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(outboxType);
        Assert.Contains(outboxType.GetIndexes(), idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "Status", "RetryCount", "CreatedAtUtc" }));

        // Notification Logs
        var notifType = context.Model.FindEntityType(typeof(NotificationLog));
        Assert.NotNull(notifType);
        Assert.Contains(notifType.GetIndexes(), idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "RecipientEmail", "IsRead", "CreatedAtUtc" }));
    }

    [Fact]
    public void DbContext_Contains_Marketplace_Discovery_And_Location_Indexes()
    {
        using var context = CreateInMemoryContext();

        // Tenant Discovery
        var tenantType = context.Model.FindEntityType(typeof(Tenant));
        Assert.NotNull(tenantType);
        Assert.Contains(tenantType.GetIndexes(), idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "IsActive", "IsPublished", "ModerationStatus", "City", "AverageRating" }));

        // Location Branches
        var locationType = context.Model.FindEntityType(typeof(Location));
        Assert.NotNull(locationType);
        Assert.Contains(locationType.GetIndexes(), idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "IsActive", "IsArchived" }));

        // Working Hours
        var whType = context.Model.FindEntityType(typeof(WorkingHours));
        Assert.NotNull(whType);
        Assert.Contains(whType.GetIndexes(), idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "StaffId", "DayOfWeek" }));
    }

    [Fact]
    public async Task QueryExecution_On_Indexed_Shapes_Returns_Deterministic_Slices()
    {
        using var context = CreateInMemoryContext();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Seed 10 sample bookings
        for (int i = 0; i < 10; i++)
        {
            var booking = new Booking(tenantId, staffId, Guid.NewGuid(), customerId, now.AddHours(i), now.AddHours(i + 1))
            {
                LocationId = locationId
            };
            if (i % 2 == 0) booking.Confirm();
            context.Bookings.Add(booking);
        }

        await context.SaveChangesAsync();

        // Test 1: Provider agenda date range query
        var agenda = await context.Bookings
            .Where(b => b.TenantId == tenantId && b.LocationId == locationId && b.StartUtc >= now && b.StartUtc <= now.AddHours(5))
            .OrderBy(b => b.StartUtc)
            .ToListAsync();

        Assert.Equal(6, agenda.Count);

        // Test 2: Customer appointment history ordered newest first
        var history = await context.Bookings
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync();

        Assert.Equal(10, history.Count);
        Assert.True(history[0].StartUtc > history[1].StartUtc);

        // Test 3: Staff availability conflict check for non-cancelled
        var conflicts = await context.Bookings
            .Where(b => b.StaffId == staffId && b.Status != BookingStatus.Cancelled && b.StartUtc < now.AddHours(3) && b.EndUtc > now.AddHours(1))
            .ToListAsync();

        Assert.NotEmpty(conflicts);
    }
}
