namespace Bookline.Application.Bookings.Commands;

using Bookline.Application.Bookings.DTOs;
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
    private readonly NodaTime.IClock _clock;

    public HoldSlotCommandHandler(
        ISlotHoldService slotHoldService,
        ITenantContext tenantContext,
        IApplicationDbContext context,
        NodaTime.IClock? clock = null)
    {
        _slotHoldService = slotHoldService;
        _tenantContext = tenantContext;
        _context = context;
        _clock = clock ?? NodaTime.SystemClock.Instance;
    }

    public async Task<HoldSlotResultDto> Handle(HoldSlotCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        var holdDurationMinutes = 5;

        if (_context != null)
        {
            var service = await _context.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
            if (service != null && (!service.IsActive || service.IsArchived || !service.IsPublished || !service.IsOnlineBookingEnabled))
            {
                throw new ValidationException("SERVICE_NOT_BOOKABLE: This service is unavailable for online booking.");
            }

            if (tenantId == Guid.Empty && service != null)
            {
                tenantId = service.TenantId;
            }

            if (tenantId != Guid.Empty)
            {
                var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
                if (tenant != null && tenant.HoldDurationMinutes > 0)
                {
                    holdDurationMinutes = tenant.HoldDurationMinutes;
                }
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
        return new HoldSlotResultDto(
            HoldId: holdId.Value,
            ExpiresAtUtc: expiresInstant.ToDateTimeOffset(),
            Slot: request.StartUtc.ToString("o"),
            Status: "HELD"
        );
    }
}
