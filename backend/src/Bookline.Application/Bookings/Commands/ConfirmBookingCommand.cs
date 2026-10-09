namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record ConfirmBookingCommand(Guid BookingId) : IRequest;

public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand>
{
    private readonly IApplicationDbContext _context;

    public ConfirmBookingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        booking.Confirm();

        // Section 90 & 91: Record Audit & Outbox event
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = "Provider/System",
            Action = "Booking.Confirmed",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                ConfirmedAtUtc = DateTimeOffset.UtcNow
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentConfirmed,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                TenantId = booking.TenantId,
                CustomerId = booking.CustomerId,
                StaffId = booking.StaffId,
                ServiceId = booking.ServiceId,
                StartUtc = booking.StartUtc,
                EndUtc = booking.EndUtc
            })
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}