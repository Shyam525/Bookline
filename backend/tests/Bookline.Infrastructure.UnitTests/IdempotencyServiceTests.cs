using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

/// <summary>
/// Unit tests verifying Section 121: Idempotency for critical operations.
/// Safely repeatable booking, payment, order creation, refund, and webhook processing.
/// </summary>
public class IdempotencyServiceTests
{
    private BooklineDbContext CreateDbContext(string dbName)
    {
        var tenantContext = new TenantContext();
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task GetExistingAsync_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var service = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        // Act
        var result = await service.GetExistingAsync("idem-key-123", "CreateBooking");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetExistingAsync_WithBlankKey_ShouldReturnNull()
    {
        // Arrange
        var db = CreateDbContext(Guid.NewGuid().ToString());
        var service = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        // Act
        var result = await service.GetExistingAsync("", "CreateBooking");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetExistingAsync_ShouldReturnCachedResult()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var service = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        var key = "idem-booking-001";
        var operation = "CreateBooking";
        var responseJson = "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"reference\":\"BL-XYZ\"}";

        // Act
        await service.SaveAsync(key, operation, 201, responseJson);

        var cached = await service.GetExistingAsync(key, operation);

        // Assert
        Assert.NotNull(cached);
        Assert.Equal(201, cached.StatusCode);
        Assert.Equal(responseJson, cached.ResponseJson);
    }

    [Fact]
    public async Task GetExistingAsync_WhenExpired_ShouldReturnNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var service = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        var key = "idem-expired-001";
        var operation = "ProcessPayment";

        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            Operation = operation,
            StatusCode = 200,
            ResponseJson = "{\"status\":\"paid\"}",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5) // already expired
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetExistingAsync(key, operation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_DifferentOperationsWithSameKey_ShouldStoreSeparately()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var service = new IdempotencyService(db, NullLogger<IdempotencyService>.Instance);

        var key = "idem-shared-key";
        await service.SaveAsync(key, "CreateBooking", 201, "{\"type\":\"booking\"}");
        await service.SaveAsync(key, "ProcessPayment", 200, "{\"type\":\"payment\"}");

        // Act
        var bookingResult = await service.GetExistingAsync(key, "CreateBooking");
        var paymentResult = await service.GetExistingAsync(key, "ProcessPayment");

        // Assert
        Assert.NotNull(bookingResult);
        Assert.Equal(201, bookingResult.StatusCode);
        Assert.NotNull(paymentResult);
        Assert.Equal(200, paymentResult.StatusCode);
    }
}
