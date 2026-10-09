namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record CancelBookingCommand(
    Guid BookingId,
    string? Reason = null,
    string? Actor = null
) : IRequest;

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

        var actor = request.Actor ?? "Customer";
        var reason = request.Reason ?? "Customer requested cancellation";

        booking.Cancel(reason, actor);

        // Record Audit log & Outbox event within atomic transaction (Section 67)
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = actor,
            Action = "Booking.Cancelled",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                Reason = reason,
                CancelledAtUtc = DateTimeOffset.UtcNow
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentCancelled,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerId = booking.CustomerId,
                StaffId = booking.StaffId,
                Reason = reason,
                CancelledBy = actor
            })
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}