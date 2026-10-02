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
}
