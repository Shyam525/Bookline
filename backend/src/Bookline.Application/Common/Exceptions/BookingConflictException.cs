namespace Bookline.Application.Common.Exceptions;

using Bookline.Application.Common.Models;

public class BookingConflictException : Exception
{
    public IReadOnlyList<Slot> NearestFreeSlots { get; }

    public BookingConflictException(IReadOnlyList<Slot> nearestFreeSlots)
        : base("The requested slot is no longer available due to a booking conflict.")
    {
        NearestFreeSlots = nearestFreeSlots ?? Array.Empty<Slot>();
    }

    public BookingConflictException(string message)
        : base(message)
    {
        NearestFreeSlots = Array.Empty<Slot>();
    }

    public BookingConflictException(string message, Exception? innerException)
        : base(message, innerException)
    {
        NearestFreeSlots = Array.Empty<Slot>();
    }
}
