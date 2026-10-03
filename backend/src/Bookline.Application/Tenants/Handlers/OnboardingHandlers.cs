using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.Tenants.Handlers;

public class OnboardingHandlers :
    IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>,
    IRequestHandler<SaveOnboardingStepCommand, OnboardingStatusDto>,
    IRequestHandler<CompleteOnboardingCommand, OnboardingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public OnboardingHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        return MapToDto(tenant);
    }

    public async Task<OnboardingStatusDto> Handle(SaveOnboardingStepCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        // Progress saved server-side
        tenant.OnboardingStep = Math.Max(tenant.OnboardingStep, req.Step + 1);

        if (!string.IsNullOrWhiteSpace(req.BusinessName)) tenant.Name = req.BusinessName;
        if (!string.IsNullOrWhiteSpace(req.BusinessType)) tenant.BusinessType = req.BusinessType;
        if (!string.IsNullOrWhiteSpace(req.Address)) tenant.Address = req.Address;
        if (!string.IsNullOrWhiteSpace(req.TimeZoneId)) tenant.TimeZoneId = req.TimeZoneId;
        if (!string.IsNullOrWhiteSpace(req.Currency)) tenant.Currency = req.Currency;

        if (req.HoldDurationMinutes.HasValue) tenant.HoldDurationMinutes = req.HoldDurationMinutes.Value;
        if (req.MinimumNoticeHours.HasValue) tenant.MinimumNoticeHours = req.MinimumNoticeHours.Value;
        if (req.BookingHorizonDays.HasValue) tenant.BookingHorizonDays = req.BookingHorizonDays.Value;

        // Step 6: First Service
        if (req.Step == 6 && !string.IsNullOrWhiteSpace(req.FirstServiceName))
        {
            var existingService = await _context.Services.FirstOrDefaultAsync(s => s.TenantId == tenant.Id, cancellationToken);
            if (existingService == null)
            {
                _context.Services.Add(new Service
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Name = req.FirstServiceName,
                    DurationMinutes = req.FirstServiceDurationMinutes ?? 45,
                    Price = req.FirstServicePrice ?? 50.00m,
                    BufferMinutes = 15,
                    IsActive = true
                });
            }
        }

        // Step 7: First Staff
        if (req.Step == 7 && !string.IsNullOrWhiteSpace(req.FirstStaffName))
        {
            var existingStaff = await _context.Staff.FirstOrDefaultAsync(s => s.TenantId == tenant.Id, cancellationToken);
            if (existingStaff == null)
            {
                _context.Staff.Add(new DomainStaff
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Name = req.FirstStaffName,
                    TimeZoneId = tenant.TimeZoneId,
                    IsActive = true
                });
            }
        }

        // Step 8: Working Hours
        if (req.Step == 8 && req.StartTime.HasValue && req.EndTime.HasValue)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.TenantId == tenant.Id, cancellationToken);
            if (staff != null)
            {
                var existingHours = await _context.WorkingHours.Where(w => w.StaffId == staff.Id).ToListAsync(cancellationToken);
                if (!existingHours.Any())
                {
                    for (int day = 1; day <= 5; day++)
                    {
                        _context.WorkingHours.Add(new WorkingHours
                        {
                            TenantId = tenant.Id,
                            StaffId = staff.Id,
                            DayOfWeek = (DayOfWeek)day,
                            StartTime = req.StartTime.Value,
                            EndTime = req.EndTime.Value
                        });
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(tenant);
    }

    public async Task<OnboardingStatusDto> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        tenant.IsOnboardingCompleted = true;
        tenant.IsActive = true;
        tenant.OnboardingStep = 10;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(tenant);
    }

    private static OnboardingStatusDto MapToDto(Tenant tenant)
    {
        return new OnboardingStatusDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.BusinessType,
            tenant.Address,
            tenant.TimeZoneId,
            tenant.Currency,
            tenant.OnboardingStep,
            tenant.IsOnboardingCompleted,
            tenant.HoldDurationMinutes,
            tenant.MinimumNoticeHours,
            tenant.BookingHorizonDays
        );
    }
}
