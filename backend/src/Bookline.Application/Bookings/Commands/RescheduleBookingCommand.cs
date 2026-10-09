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
    Guid HoldId) : IRequest<BookingDto>;

public class RescheduleBookingCommandValidator : AbstractValidator<RescheduleBookingCommand>
{
    public RescheduleBookingCommandValidator()
    {
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.HoldId).NotEmpty();
        RuleFor(command => command.StartUtc).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public class RescheduleBookingCommandHandler : IRequestHandler<RescheduleBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlotHoldService _slotHoldService;

    public RescheduleBookingCommandHandler(
        IApplicationDbContext context,
        ISlotHoldService slotHoldService)
    {
        _context = context;
        _slotHoldService = slotHoldService;
    }

    public async Task<BookingDto> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(candidate => candidate.Id == request.BookingId, cancellationToken);
        if (booking == null)
        {
            throw new NotFoundException(nameof(Booking), request.BookingId);
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(candidate => candidate.Id == booking.ServiceId, cancellationToken);
        if (service == null)
        {
            throw new NotFoundException(nameof(Service), booking.ServiceId);
        }

        var validHold = await _slotHoldService.ValidateHoldAsync(
            booking.TenantId,
            booking.StaffId,
            request.StartUtc,
            request.HoldId,
            cancellationToken);
        if (!validHold)
        {
            throw new ValidationException("Slot hold is invalid or has expired.");
        }

        var endUtc = request.StartUtc.AddMinutes(service.DurationMinutes);
        booking.Reschedule(request.StartUtc, endUtc);

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
            await _slotHoldService.ReleaseHoldAsync(
                booking.TenantId,
                booking.StaffId,
                request.StartUtc,
                request.HoldId,
                cancellationToken);
            throw new BookingConflictException(Array.Empty<Slot>());
        }

        await _slotHoldService.ReleaseHoldAsync(
            booking.TenantId,
            booking.StaffId,
            request.StartUtc,
            request.HoldId,
            cancellationToken);

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
            if (type.Name == "PostgresException" &&
                type.GetProperty("SqlState")?.GetValue(inner)?.ToString() == "23P01")
            {
                return true;
            }

            inner = inner.InnerException;
        }

        return false;
    }
}