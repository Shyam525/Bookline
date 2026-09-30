namespace Bookline.Domain.Exceptions;

using Bookline.Domain.Entities;

public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(BookingStatus current, BookingStatus target)
        : base($"Cannot transition booking status from '{current}' to '{target}'.")
    {
    }
}
