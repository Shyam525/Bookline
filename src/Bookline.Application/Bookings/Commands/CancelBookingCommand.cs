namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record CancelBookingCommand(Guid BookingId) : IRequest;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _context;

    public CancelBookingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        booking.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
    }
}