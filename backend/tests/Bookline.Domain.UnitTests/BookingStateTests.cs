namespace Bookline.Domain.UnitTests;

using Xunit;
using Bookline.Domain.Entities;
using Bookline.Domain.Exceptions;


public class BookingStateTests
{
    [Fact]
    public void NewBooking_StartsWithPendingStatus()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public void PendingBooking_CanTransitionToConfirmedOrCancelled()
    {
        var booking1 = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        booking1.Confirm();
        Assert.Equal(BookingStatus.Confirmed, booking1.Status);

        var booking2 = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        booking2.Cancel();
        Assert.Equal(BookingStatus.Cancelled, booking2.Status);
    }

    [Fact]
    public void ConfirmedBooking_CanTransitionToCompletedCancelledOrNoShow()
    {
        var booking1 = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        booking1.Confirm();
        booking1.Complete();
        Assert.Equal(BookingStatus.Completed, booking1.Status);

        var booking2 = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        booking2.Confirm();
        booking2.Cancel();
        Assert.Equal(BookingStatus.Cancelled, booking2.Status);

        var booking3 = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        booking3.Confirm();
        booking3.MarkNoShow();
        Assert.Equal(BookingStatus.NoShow, booking3.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.NoShow)]
    public void TerminalStatus_CannotTransitionToConfirmed(BookingStatus initialStatus)
    {
        var booking = CreateBookingWithStatus(initialStatus);

        Assert.Throws<InvalidStatusTransitionException>(() => booking.Confirm());
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.NoShow)]
    public void TerminalStatus_CannotTransitionToCompleted(BookingStatus initialStatus)
    {
        var booking = CreateBookingWithStatus(initialStatus);

        Assert.Throws<InvalidStatusTransitionException>(() => booking.Complete());
    }

    [Fact]
    public void CompletedOrNoShow_CannotTransitionToCancelled()
    {
        var completedBooking = CreateBookingWithStatus(BookingStatus.Completed);
        Assert.Throws<InvalidStatusTransitionException>(() => completedBooking.Cancel());

        var noShowBooking = CreateBookingWithStatus(BookingStatus.NoShow);
        Assert.Throws<InvalidStatusTransitionException>(() => noShowBooking.Cancel());
    }

    private static Booking CreateBookingWithStatus(BookingStatus status)
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;
            case BookingStatus.Completed:
                booking.Confirm();
                booking.Complete();
                break;
            case BookingStatus.Cancelled:
                booking.Cancel();
                break;
            case BookingStatus.NoShow:
                booking.Confirm();
                booking.MarkNoShow();
                break;
        }
        return booking;
    }
}
