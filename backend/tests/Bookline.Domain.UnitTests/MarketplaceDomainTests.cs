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

    [Fact]
    public void Refund_Entity_ShouldTrackAllRequiredProperties()
    {
        // Section 87: Track payment, refund, amount, currency, status, external reference
        var paymentId = Guid.NewGuid();
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = 150.00m,
            Currency = "INR",
            Status = "Succeeded",
            ExternalReference = "REF-TEST-84920",
            Reason = "Customer request within cancellation window"
        };

        Assert.Equal(paymentId, refund.PaymentId);
        Assert.Equal(150.00m, refund.Amount);
        Assert.Equal("INR", refund.Currency);
        Assert.Equal("Succeeded", refund.Status);
        Assert.Equal("REF-TEST-84920", refund.ExternalReference);
        Assert.NotNull(refund.Reason);
        Assert.True(refund.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void NotificationEvents_ShouldDefineAllTenMandatoryEvents()
    {
        // Section 90: Explicit event set
        Assert.Equal("AppointmentCreated", Bookline.Domain.Constants.NotificationEvents.AppointmentCreated);
        Assert.Equal("AppointmentConfirmed", Bookline.Domain.Constants.NotificationEvents.AppointmentConfirmed);
        Assert.Equal("AppointmentCancelled", Bookline.Domain.Constants.NotificationEvents.AppointmentCancelled);
        Assert.Equal("AppointmentRescheduled", Bookline.Domain.Constants.NotificationEvents.AppointmentRescheduled);
        Assert.Equal("ReminderDue", Bookline.Domain.Constants.NotificationEvents.ReminderDue);
        Assert.Equal("OrderCreated", Bookline.Domain.Constants.NotificationEvents.OrderCreated);
        Assert.Equal("PaymentSucceeded", Bookline.Domain.Constants.NotificationEvents.PaymentSucceeded);
        Assert.Equal("PaymentFailed", Bookline.Domain.Constants.NotificationEvents.PaymentFailed);
        Assert.Equal("RefundCreated", Bookline.Domain.Constants.NotificationEvents.RefundCreated);
        Assert.Equal("ReviewCreated", Bookline.Domain.Constants.NotificationEvents.ReviewCreated);
    }

    [Fact]
    public void Product_Purchase_ShouldNeverAllowNegativeInventory()
    {
        // Section 84: Do not allow negative inventory
        var product = new Product
        {
            Name = "Hair Serum",
            Price = 25.00m,
            StockQuantity = 2,
            ReservedQuantity = 0
        };

        Assert.True(product.Purchase(2));
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(0, product.AvailableQuantity);
        Assert.Equal(2, product.SoldQuantity);

        // Attempt to purchase 1 more when available is 0
        Assert.False(product.Purchase(1));
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(0, product.AvailableQuantity);
        Assert.Equal(2, product.SoldQuantity);
    }

    [Fact]
    public void ProviderTeamRole_ShouldDefineCanonicalSixRolesInCorrectHierarchy()
    {
        // Section 95: Owner, Admin, Manager, Receptionist, Staff, Viewer
        Assert.Equal(0, (int)ProviderTeamRole.Owner);
        Assert.Equal(1, (int)ProviderTeamRole.Admin);
        Assert.Equal(2, (int)ProviderTeamRole.Manager);
        Assert.Equal(3, (int)ProviderTeamRole.Receptionist);
        Assert.Equal(4, (int)ProviderTeamRole.Staff);
        Assert.Equal(5, (int)ProviderTeamRole.Viewer);
    }

    [Fact]
    public void ProviderVerificationStatus_ShouldDefineCanonicalFourStates()
    {
        // Section 97: Unverified, Pending, Verified, Suspended
        Assert.Equal(VerificationStatus.Unverified, Enum.Parse<VerificationStatus>("Unverified"));
        Assert.Equal(VerificationStatus.Pending, Enum.Parse<VerificationStatus>("Pending"));
        Assert.Equal(VerificationStatus.Verified, Enum.Parse<VerificationStatus>("Verified"));
        Assert.Equal(VerificationStatus.Suspended, Enum.Parse<VerificationStatus>("Suspended"));
    }

    [Fact]
    public void ModerationStatus_ShouldDefineCanonicalFourStates()
    {
        // Section 98: Pending, Approved, Rejected, Suspended
        Assert.Equal(ModerationStatus.Pending, Enum.Parse<ModerationStatus>("Pending"));
        Assert.Equal(ModerationStatus.Approved, Enum.Parse<ModerationStatus>("Approved"));
        Assert.Equal(ModerationStatus.Rejected, Enum.Parse<ModerationStatus>("Rejected"));
        Assert.Equal(ModerationStatus.Suspended, Enum.Parse<ModerationStatus>("Suspended"));
    }
}
