namespace Bookline.Infrastructure.UnitTests;

using System.Security.Cryptography;
using System.Text;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class Phase6Tests
{
    private readonly DbContextOptions<BooklineDbContext> _options;

    public Phase6Tests()
    {
        _options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool IsResolved => true;
        public void SetTenantId(Guid tenantId) => TenantId = tenantId;
    }

    [Fact]
    public void ComputeSignature_ShouldGenerateValidHmacSha256Header()
    {
        // Arrange
        var context = new BooklineDbContext(_options, new TestTenantContext());
        var dispatcher = new WebhookDispatcher(context);

        var json = "{\"bookingId\":\"11111111-1111-1111-1111-111111111111\"}";
        var secret = "my_secret_key_123";
        var timestamp = 1700000000L;

        // Act
        var header = dispatcher.ComputeSignature(json, secret, timestamp);

        // Assert
        Assert.StartsWith("t=1700000000,v1=", header);

        // Verify HMAC calculation manually
        var expectedToSign = $"1700000000.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHashHex = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedToSign))).Replace("-", "").ToLowerInvariant();

        Assert.Equal($"t=1700000000,v1={expectedHashHex}", header);
    }

    [Fact]
    public async Task CleanupStalePendingBookingsJob_ShouldCancelPendingBookingsOlderThan15Minutes()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TestTenantContext { TenantId = tenantId };
        using var context = new BooklineDbContext(_options, tenantContext);

        var staleBooking = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow.AddHours(3))
        {
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20)
        };

        var freshBooking = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(4), DateTimeOffset.UtcNow.AddHours(5))
        {
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        context.Bookings.AddRange(staleBooking, freshBooking);
        await context.SaveChangesAsync();

        var job = new CleanupStalePendingBookingsJob(context);

        // Act
        await job.ExecuteAsync();

        // Assert
        var updatedStale = await context.Bookings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == staleBooking.Id);
        var updatedFresh = await context.Bookings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == freshBooking.Id);

        Assert.NotNull(updatedStale);
        Assert.Equal(BookingStatus.Cancelled, updatedStale.Status);

        Assert.NotNull(updatedFresh);
        Assert.Equal(BookingStatus.Pending, updatedFresh.Status);
    }

    [Fact]
    public void Booking_MarkReminderSent_ShouldSetTimestamp()
    {
        // Arrange
        var booking = new Booking();
        var sentAt = DateTimeOffset.UtcNow;

        // Act
        booking.MarkReminderSent(sentAt);

        // Assert
        Assert.Equal(sentAt, booking.ReminderSentAtUtc);
    }
}
