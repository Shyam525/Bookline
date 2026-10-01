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

    public HoldSlotCommandHandler(ISlotHoldService slotHoldService, ITenantContext tenantContext, IApplicationDbContext context)
    {
        _slotHoldService = slotHoldService;
        _tenantContext = tenantContext;
        _context = context;
    }

    public async Task<HoldSlotResultDto> Handle(HoldSlotCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            var service = await _context.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
            if (service != null)
            {
                tenantId = service.TenantId;
            }
        }

        var holdId = await _slotHoldService.AcquireHoldAsync(
            tenantId,
            request.StaffId,
            request.StartUtc,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        if (holdId == null)
        {
            throw new ValidationException("This slot is currently held by another user or unavailable.");
        }

        return new HoldSlotResultDto(holdId.Value, DateTimeOffset.UtcNow.AddSeconds(300));
    }
}
