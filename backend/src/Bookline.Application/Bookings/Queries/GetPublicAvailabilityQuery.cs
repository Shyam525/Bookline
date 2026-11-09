namespace Bookline.Application.Bookings.Queries;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public record GetPublicAvailabilityQuery(
    Guid StaffId,
    Guid ServiceId,
    LocalDate Date,
    string TimeZoneId,
    string? Slug = null
) : IRequest<IReadOnlyList<Slot>>;

public class GetPublicAvailabilityQueryValidator : AbstractValidator<GetPublicAvailabilityQuery>
{
    public GetPublicAvailabilityQueryValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.TimeZoneId).NotEmpty();
    }
}

public class GetPublicAvailabilityQueryHandler : IRequestHandler<GetPublicAvailabilityQuery, IReadOnlyList<Slot>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlotEngine _slotEngine;
    private readonly IClock _clock;

    public GetPublicAvailabilityQueryHandler(IApplicationDbContext context, ISlotEngine slotEngine, IClock? clock = null)
    {
        _context = context;
        _slotEngine = slotEngine;
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<IReadOnlyList<Slot>> Handle(GetPublicAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), request.ServiceId);
        }

        if (!service.IsActive || service.IsArchived || !service.IsPublished || !service.IsOnlineBookingEnabled)
        {
            throw new ValidationException("SERVICE_NOT_BOOKABLE: The requested service is not bookable online.");
        }

        var serviceTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == service.TenantId, cancellationToken);

        if (serviceTenant != null && (!serviceTenant.IsActive || serviceTenant.VerificationStatus == Bookline.Domain.Enums.VerificationStatus.Suspended || serviceTenant.ModerationStatus == Bookline.Domain.Enums.ModerationStatus.Suspended))
        {
            throw new ValidationException("PROVIDER_SUSPENDED: The provider is suspended or not accepting public bookings.");
        }

        var staff = await _context.Staff
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Staff), request.StaffId);
        }

        if (service.TenantId != staff.TenantId)
        {
            throw new NotFoundException("Service and Staff belong to different tenants.");
        }

        var hasAssignedStaff = await _context.StaffServices
            .IgnoreQueryFilters()
            .AnyAsync(ss => ss.ServiceId == service.Id, cancellationToken);

        if (hasAssignedStaff)
        {
            var isStaffAssigned = await _context.StaffServices
                .IgnoreQueryFilters()
                .AnyAsync(ss => ss.StaffId == request.StaffId && ss.ServiceId == service.Id, cancellationToken);

            if (!isStaffAssigned)
            {
                throw new ValidationException("The selected staff member is not eligible to perform this service.");
            }
        }

        Tenant? tenant = null;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug.ToLower() == request.Slug.ToLower() && t.IsActive, cancellationToken);

            if (tenant == null || service.TenantId != tenant.Id || staff.TenantId != tenant.Id)
            {
                throw new NotFoundException("Service or Staff does not belong to the specified tenant.");
            }
        }
        else
        {
            tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == service.TenantId, cancellationToken);
        }

        DateTimeZone zone;
        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            var resolved = DateTimeZoneProviders.Tzdb.GetZoneOrNull(request.TimeZoneId.Trim());
            if (resolved == null)
            {
                throw new ValidationException($"INVALID_TIMEZONE: '{request.TimeZoneId}' is not a valid IANA timezone identifier.");
            }
            zone = resolved;
        }
        else
        {
            zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(staff.TimeZoneId)
                ?? (!string.IsNullOrWhiteSpace(tenant?.TimeZoneId) ? DateTimeZoneProviders.Tzdb.GetZoneOrNull(tenant.TimeZoneId) : null)
                ?? DateTimeZoneProviders.Tzdb["UTC"];
        }

        var targetDayOfWeek = (DayOfWeek)(((int)request.Date.DayOfWeek) % 7);

        // 1. Validate Booking Horizon
        if (tenant != null && tenant.BookingHorizonDays > 0)
        {
            var localToday = _clock.GetCurrentInstant().InZone(zone).Date;
            if (request.Date > localToday.PlusDays(tenant.BookingHorizonDays))
            {
                return Array.Empty<Slot>();
            }
        }

        var workingHours = await _context.WorkingHours
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.StaffId == request.StaffId)
            .ToListAsync(cancellationToken);

        var existingBookings = await _context.Bookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.StaffId == request.StaffId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
            .ToListAsync(cancellationToken);

        var timeOffs = await _context.TimeOffs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.StaffId == request.StaffId)
            .ToListAsync(cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var activeHolds = await _context.BookingHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(h => h.StaffId == request.StaffId && !h.IsReleased && h.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        var dayHours = workingHours.Where(w => w.DayOfWeek == targetDayOfWeek).ToList();
        var windows = dayHours.Select(w => new WorkingWindow(
            LocalTime.FromTicksSinceMidnight(w.StartTime.Ticks),
            LocalTime.FromTicksSinceMidnight(w.EndTime.Ticks)
        )).ToList();

        // 2. Business Opening Hours, Closed Days & Business Break Intervals
        var businessBreakIntervals = new List<Interval>();
        if (!string.IsNullOrWhiteSpace(tenant?.BusinessHoursJson))
        {
            try
            {
                var businessHours = System.Text.Json.JsonSerializer.Deserialize<List<Bookline.Application.Tenants.DTOs.BusinessDayHours>>(
                    tenant.BusinessHoursJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var dayConfig = businessHours?.FirstOrDefault(d => d.DayOfWeek == targetDayOfWeek);
                if (dayConfig != null)
                {
                    if (!dayConfig.IsOpen)
                    {
                        return Array.Empty<Slot>();
                    }

                    if (dayConfig.OpenTime.HasValue && dayConfig.CloseTime.HasValue)
                    {
                        var bOpen = LocalTime.FromTicksSinceMidnight(dayConfig.OpenTime.Value.ToTimeSpan().Ticks);
                        var bClose = LocalTime.FromTicksSinceMidnight(dayConfig.CloseTime.Value.ToTimeSpan().Ticks);

                        var clamped = new List<WorkingWindow>();
                        foreach (var w in windows)
                        {
                            var wStart = w.Start > bOpen ? w.Start : bOpen;
                            var wEnd = w.End < bClose ? w.End : bClose;
                            if (wEnd > wStart)
                            {
                                clamped.Add(new WorkingWindow(wStart, wEnd));
                            }
                        }
                        windows = clamped;
                    }

                    if (dayConfig.Breaks != null && dayConfig.Breaks.Any())
                    {
                        foreach (var b in dayConfig.Breaks)
                        {
                            var bStart = LocalTime.FromTicksSinceMidnight(b.StartTime.ToTimeSpan().Ticks);
                            var bEnd = LocalTime.FromTicksSinceMidnight(b.EndTime.ToTimeSpan().Ticks);
                            var bStartInstant = request.Date.At(bStart).InZoneLeniently(zone).ToInstant();
                            var bEndInstant = request.Date.At(bEnd).InZoneLeniently(zone).ToInstant();
                            if (bEndInstant > bStartInstant)
                            {
                                businessBreakIntervals.Add(new Interval(bStartInstant, bEndInstant));
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Fallback gracefully on parsing anomaly
            }
        }

        var totalBufferMinutes = service.BufferBeforeMinutes + service.BufferAfterMinutes;
        var serviceInfo = new ServiceInfo(
            Duration.FromMinutes(service.DurationMinutes),
            Duration.FromMinutes(totalBufferMinutes));

        var schedule = new StaffSchedule(windows);

        var bookingIntervals = existingBookings.Select(b => new Interval(
            Instant.FromDateTimeOffset(b.StartUtc),
            Instant.FromDateTimeOffset(b.EndUtc)
        )).ToList();

        // Active booking holds block availability
        foreach (var hold in activeHolds)
        {
            bookingIntervals.Add(new Interval(
                Instant.FromDateTimeOffset(hold.StartUtc),
                Instant.FromDateTimeOffset(hold.EndUtc)
            ));
        }

        var timeOffIntervals = timeOffs.Select(t => new Interval(
            Instant.FromDateTimeOffset(t.StartUtc),
            Instant.FromDateTimeOffset(t.EndUtc)
        )).ToList();

        // Business breaks are treated as blocked intervals
        timeOffIntervals.AddRange(businessBreakIntervals);

        var minNotice = Duration.FromHours(tenant?.MinimumNoticeHours ?? 2);

        return _slotEngine.Compute(
            serviceInfo,
            schedule,
            bookingIntervals,
            timeOffIntervals,
            request.Date,
            zone,
            _clock.GetCurrentInstant(),
            Duration.FromMinutes(15),
            minNotice);
    }
}
