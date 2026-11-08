namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record HoldSlotCommand(
    Guid StaffId,
    Guid ServiceId,
    DateTimeOffset StartUtc
) : IRequest<HoldSlotResultDto>;

public class HoldSlotCommandValidator : AbstractValidator<HoldSlotCommand>
{
    public HoldSlotCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StartUtc).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1))
            .WithMessage("Start time must be in the future.");
    }
}

public class HoldSlotCommandHandler : IRequestHandler<HoldSlotCommand, HoldSlotResultDto>
{
    private readonly ISlotHoldService _slotHoldService;
    private readonly ITenantContext _tenantContext;
    private readonly IApplicationDbContext _context;
    private readonly IAppointmentSchedulingService? _schedulingService;
    private readonly NodaTime.IClock _clock;

    public HoldSlotCommandHandler(
        ISlotHoldService slotHoldService,
        ITenantContext tenantContext,
        IApplicationDbContext context,
        NodaTime.IClock? clock = null,
        IAppointmentSchedulingService? schedulingService = null)
    {
        _slotHoldService = slotHoldService;
        _tenantContext = tenantContext;
        _context = context;
        _clock = clock ?? NodaTime.SystemClock.Instance;
        _schedulingService = schedulingService;
    }

    public async Task<HoldSlotResultDto> Handle(HoldSlotCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        var holdDurationMinutes = 5;

        DateTimeOffset endUtc = request.StartUtc.AddMinutes(30);

        if (_context != null)
        {
            var service = await _context.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
            if (service == null)
            {
                throw new NotFoundException(nameof(Bookline.Domain.Entities.Service), request.ServiceId);
            }

            if (!service.IsActive || service.IsArchived || !service.IsPublished || !service.IsOnlineBookingEnabled)
            {
                throw new ValidationException("SERVICE_NOT_BOOKABLE: This service is unavailable for online booking.");
            }

            var staff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);
            if (staff == null || !staff.IsActive || staff.IsArchived)
            {
                throw new ValidationException("STAFF_UNAVAILABLE: The selected staff member is inactive or unavailable.");
            }

            if (tenantId == Guid.Empty)
            {
                tenantId = service.TenantId;
            }

            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
            if (tenant != null && tenant.HoldDurationMinutes > 0)
            {
                holdDurationMinutes = tenant.HoldDurationMinutes;
            }

            endUtc = request.StartUtc.AddMinutes(service.DurationMinutes);

            // Validate schedule eligibility before placing hold
            if (_schedulingService != null)
            {
                await _schedulingService.ValidateScheduleEligibilityAsync(
                    tenantId: tenantId,
                    staffId: request.StaffId,
                    serviceId: request.ServiceId,
                    startUtc: request.StartUtc,
                    endUtc: endUtc,
                    locationId: null,
                    excludeBookingId: null,
                    allowPast: false,
                    cancellationToken: cancellationToken);
            }
        }

        var holdDuration = NodaTime.Duration.FromMinutes(holdDurationMinutes);

        var holdId = await _slotHoldService.AcquireHoldAsync(
            tenantId,
            request.StaffId,
            request.StartUtc,
            holdDuration.ToTimeSpan(),
            cancellationToken);

        if (holdId == null)
        {
            throw new ValidationException("SLOT_UNAVAILABLE: This slot is currently held by another user or unavailable.");
        }

        var expiresInstant = _clock.GetCurrentInstant() + holdDuration;
        var expiresAtUtc = expiresInstant.ToDateTimeOffset();

        // Persist hold record to database for audit & background worker cleanup
        if (_context != null)
        {
            _context.BookingHolds.Add(new Bookline.Domain.Entities.BookingHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = request.StaffId,
                ServiceId = request.ServiceId,
                StartUtc = request.StartUtc,
                EndUtc = endUtc,
                HoldToken = holdId.Value.ToString("N"),
                ExpiresAtUtc = expiresAtUtc,
                IsReleased = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return new HoldSlotResultDto(
            HoldId: holdId.Value,
            ExpiresAtUtc: expiresAtUtc,
            Slot: request.StartUtc.ToString("o"),
            Status: "HELD"
        );
    }
}
