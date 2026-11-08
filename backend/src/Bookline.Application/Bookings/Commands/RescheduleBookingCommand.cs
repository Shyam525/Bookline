namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record RescheduleBookingCommand(
    Guid BookingId,
    DateTimeOffset StartUtc,
    Guid? HoldId = null,
    Guid? StaffId = null) : IRequest<BookingDto>;

public class RescheduleBookingCommandValidator : AbstractValidator<RescheduleBookingCommand>
{
    public RescheduleBookingCommandValidator()
    {
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.StartUtc).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }
}

public class RescheduleBookingCommandHandler : IRequestHandler<RescheduleBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlotHoldService _slotHoldService;
    private readonly IAppointmentSchedulingService? _schedulingService;

    public RescheduleBookingCommandHandler(
        IApplicationDbContext context,
        ISlotHoldService slotHoldService,
        IAppointmentSchedulingService? schedulingService = null)
    {
        _context = context;
        _slotHoldService = slotHoldService;
        _schedulingService = schedulingService;
    }

    public async Task<BookingDto> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.BookingId, cancellationToken);
        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        var targetStaffId = request.StaffId.HasValue && request.StaffId.Value != Guid.Empty
            ? request.StaffId.Value
            : booking.StaffId;

        var service = await _context.Services
            .FirstOrDefaultAsync(candidate => candidate.Id == booking.ServiceId, cancellationToken);
        if (service == null)
        {
            throw new NotFoundException(nameof(Service), booking.ServiceId);
        }

        if (request.HoldId.HasValue && request.HoldId.Value != Guid.Empty)
        {
            var validHold = await _slotHoldService.ValidateHoldAsync(
                booking.TenantId,
                targetStaffId,
                request.StartUtc,
                request.HoldId.Value,
                cancellationToken);
            if (!validHold)
            {
                throw new ValidationException("Slot hold is invalid or has expired.");
            }
        }

        var endUtc = request.StartUtc.AddMinutes(service.DurationMinutes);

        var originalStartUtc = booking.StartUtc;
        var originalEndUtc = booking.EndUtc;
        var originalStaffId = booking.StaffId;

        // Perform conflict and schedule validation using shared scheduling rules
        if (_schedulingService != null)
        {
            try
            {
                await _schedulingService.ValidateScheduleEligibilityAsync(
                    tenantId: booking.TenantId,
                    staffId: targetStaffId,
                    serviceId: booking.ServiceId,
                    startUtc: request.StartUtc,
                    endUtc: endUtc,
                    locationId: booking.LocationId,
                    excludeBookingId: booking.Id,
                    allowPast: false,
                    cancellationToken: cancellationToken);
            }
            catch (ValidationException vex) when (vex.Message.Contains("SCHEDULING_CONFLICT"))
            {
                throw new BookingConflictException(Array.Empty<Slot>());
            }
        }

        booking.Reschedule(request.StartUtc, endUtc, request.StaffId);

        // Record Audit log & Outbox event within atomic transaction (Section 66)
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = "Customer",
            Action = "Booking.Rescheduled",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                NewStartUtc = request.StartUtc,
                NewEndUtc = endUtc,
                RescheduledAtUtc = DateTimeOffset.UtcNow
            })
        });

        _context.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentRescheduled,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerId = booking.CustomerId,
                StaffId = booking.StaffId,
                StartUtc = request.StartUtc,
                EndUtc = endUtc
            })
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsOverlapViolation(exception))
        {
            // Preserve original appointment safely on conflict
            booking.Reschedule(originalStartUtc, originalEndUtc, originalStaffId);

            if (request.HoldId.HasValue && request.HoldId.Value != Guid.Empty)
            {
                await _slotHoldService.ReleaseHoldAsync(
                    booking.TenantId,
                    targetStaffId,
                    request.StartUtc,
                    request.HoldId.Value,
                    cancellationToken);
            }
            throw new BookingConflictException(Array.Empty<Slot>());
        }

        if (request.HoldId.HasValue && request.HoldId.Value != Guid.Empty)
        {
            await _slotHoldService.ReleaseHoldAsync(
                booking.TenantId,
                booking.StaffId,
                request.StartUtc,
                request.HoldId.Value,
                cancellationToken);
        }

        return new BookingDto(
            booking.Id,
            booking.TenantId,
            booking.StaffId,
            booking.ServiceId,
            booking.CustomerId,
            booking.StartUtc,
            booking.EndUtc,
            booking.Status,
            booking.CreatedAtUtc);
    }

    private static bool IsOverlapViolation(DbUpdateException exception)
    {
        var inner = exception.InnerException;
        while (inner != null)
        {
            var type = inner.GetType();
            if (type.Name == "PostgresException")
            {
                var sqlState = type.GetProperty("SqlState")?.GetValue(inner)?.ToString();
                var constraint = type.GetProperty("ConstraintName")?.GetValue(inner)?.ToString();
                if (sqlState == "23P01" || constraint == "no_overlap" || constraint == "no_resource_overlap" || constraint == "no_overlap_staff_booking")
                {
                    return true;
                }
            }

            if (inner.Message.Contains("23P01") || inner.Message.Contains("no_overlap") || inner.Message.Contains("no_resource_overlap"))
            {
                return true;
            }

            inner = inner.InnerException;
        }

        return false;
    }
}