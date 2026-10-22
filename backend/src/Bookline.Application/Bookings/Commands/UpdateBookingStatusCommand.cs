namespace Bookline.Application.Bookings.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record UpdateBookingStatusCommand(Guid BookingId, string Status, string? Reason = null) : IRequest;

public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateBookingStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        string eventType;
        string action;

        switch (request.Status.ToLowerInvariant())
        {
            case "checkedin":
            case "checked_in":
            case "checked-in":
                booking.CheckIn();
                eventType = "AppointmentCheckedIn";
                action = "Booking.CheckedIn";
                break;

            case "completed":
                booking.Complete();
                eventType = "AppointmentCompleted";
                action = "Booking.Completed";
                break;

            case "noshow":
            case "no_show":
            case "no-show":
                booking.MarkNoShow();
                eventType = "AppointmentNoShow";
                action = "Booking.NoShow";
                break;

            case "confirmed":
                booking.Confirm();
                eventType = Bookline.Domain.Constants.NotificationEvents.AppointmentConfirmed;
                action = "Booking.Confirmed";
                break;

            case "cancelled":
            case "canceled":
                booking.Cancel(request.Reason, "Provider");
                eventType = Bookline.Domain.Constants.NotificationEvents.AppointmentCancelled;
                action = "Booking.Cancelled";
                break;

            default:
                throw new ArgumentException($"Invalid booking status '{request.Status}'. Allowed: CheckedIn, Completed, NoShow, Confirmed, Cancelled.");
        }

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = "Provider",
            Action = action,
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                Status = booking.Status.ToString(),
                Reason = request.Reason,
                TimestampUtc = DateTimeOffset.UtcNow
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = eventType,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                TenantId = booking.TenantId,
                CustomerId = booking.CustomerId,
                StaffId = booking.StaffId,
                ServiceId = booking.ServiceId,
                Status = booking.Status.ToString(),
                Reason = request.Reason
            })
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
