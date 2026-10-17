namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using FluentValidation;
using NodaTime;

public record CancelBookingCommand(
    Guid BookingId,
    string? Reason = null,
    string? Actor = null,
    bool EnforceCancellationPolicy = true
) : IRequest;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    public CancelBookingCommandHandler(IApplicationDbContext context, IClock? clock = null)
    {
        _context = context;
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == booking.TenantId, cancellationToken);

        if (request.EnforceCancellationPolicy && tenant != null && tenant.MinimumNoticeHours > 0)
        {
            var currentInstant = _clock.GetCurrentInstant();
            var bookingStartInstant = Instant.FromDateTimeOffset(booking.StartUtc);
            var cutoffDuration = Duration.FromHours(tenant.MinimumNoticeHours);

            if (bookingStartInstant < currentInstant + cutoffDuration)
            {
                throw new ValidationException($"CANCELLATION_CUTOFF_EXCEEDED: Cancellations must be made at least {tenant.MinimumNoticeHours} hours prior to appointment.");
            }
        }

        var actor = request.Actor ?? "Customer";
        var reason = request.Reason ?? "Customer requested cancellation";
        var cancelledInstant = _clock.GetCurrentInstant();

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
                CancelledAtUtc = cancelledInstant.ToDateTimeOffset()
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