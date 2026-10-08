using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Xunit;

namespace Bookline.Domain.UnitTests;

public class MarketplaceDomainTests
{
    [Fact]
    public void Commission_Calculate_ShouldSplitPlatformTakeRateAccurately()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        decimal grossAmount = 1000.00m;
        decimal takeRate = 10.00m;

        // Act
        var commission = Commission.Calculate(tenantId, grossAmount, takeRate);

        // Assert
        Assert.Equal(100.00m, commission.CommissionAmount);
        Assert.Equal(900.00m, commission.ProviderNetAmount);
        Assert.Equal(grossAmount, commission.GrossAmount);
        Assert.Equal("Collected", commission.Status);
    }

    [Fact]
    public void Product_Reserve_And_Purchase_ShouldPreventOverselling()
    {
        // Arrange
        var product = new Product
        {
            Name = "Luxury Botanical Oil",
            Price = 45.00m,
            StockQuantity = 5,
            ReservedQuantity = 0
        };

        // Act: Reserve 3
        var reserveSuccess = product.Reserve(3);

        // Assert
        Assert.True(reserveSuccess);
        Assert.Equal(2, product.AvailableQuantity);
        Assert.Equal(3, product.ReservedQuantity);

        // Act: Attempt to reserve 3 more (only 2 left)
        var secondReserve = product.Reserve(3);
        Assert.False(secondReserve);

        // Act: Purchase the 3 reserved items
        var purchaseSuccess = product.Purchase(3);
        Assert.True(purchaseSuccess);
        Assert.Equal(2, product.StockQuantity);
        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(3, product.SoldQuantity);
    }

    [Fact]
    public void BookingHold_ShouldAccuratelyDetermineActiveStatus()
    {
        // Arrange
        var activeHold = new BookingHold
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            IsReleased = false
        };

        var expiredHold = new BookingHold
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            IsReleased = false
        };

        var releasedHold = new BookingHold
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            IsReleased = true
        };

        // Assert
        Assert.True(activeHold.IsActive);
        Assert.False(expiredHold.IsActive);
        Assert.False(releasedHold.IsActive);
    }

    [Fact]
    public void Booking_CheckIn_ShouldTransitionFromConfirmedToCheckedIn()
    {
        // Arrange
        var booking = new Booking(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2)
        );
        booking.Confirm();

        // Act
        booking.CheckIn();

        // Assert
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
    }
}
