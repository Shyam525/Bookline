namespace Bookline.Application.Common.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class AppointmentSchedulingService : IAppointmentSchedulingService
{
    private readonly IApplicationDbContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AppointmentSchedulingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ValidateScheduleEligibilityAsync(
        Guid tenantId,
        Guid staffId,
        Guid serviceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? locationId = null,
        Guid? excludeBookingId = null,
        bool allowPast = false,
        CancellationToken cancellationToken = default)
    {
        if (endUtc <= startUtc)
        {
            throw new ValidationException("INVALID_DURATION: Appointment end time must be after its start time.");
        }

        // 1. Service Validation
        var service = await _context.Services
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), serviceId);
        }

        if (!service.IsActive || service.IsArchived)
        {
            throw new ValidationException("SERVICE_INACTIVE: This service is currently inactive or archived.");
        }

        if (service.DurationMinutes <= 0)
        {
            throw new ValidationException("INVALID_SERVICE_DURATION: Service duration must be greater than zero.");
        }

        // 2. Staff Validation
        var staff = await _context.Staff
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == staffId && s.TenantId == tenantId, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Staff), staffId);
        }

        if (!staff.IsActive || staff.IsArchived)
        {
            throw new ValidationException("STAFF_INACTIVE: The selected staff member is inactive or unavailable.");
        }

        // Staff Service Assignment Check
        var hasStaffAssignments = await _context.StaffServices
            .IgnoreQueryFilters()
            .AnyAsync(ss => ss.ServiceId == service.Id, cancellationToken);

        if (hasStaffAssignments)
        {
            var isAssigned = await _context.StaffServices
                .IgnoreQueryFilters()
                .AnyAsync(ss => ss.StaffId == staffId && ss.ServiceId == service.Id, cancellationToken);

            if (!isAssigned)
            {
                throw new ValidationException("STAFF_NOT_ELIGIBLE: The selected staff member is not eligible to perform this service.");
            }
        }

        // 3. Location Validation (if specified)
        if (locationId.HasValue)
        {
            var location = await _context.Locations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == locationId.Value && l.TenantId == tenantId && l.IsActive, cancellationToken);

            if (location == null)
            {
                throw new ValidationException("LOCATION_INVALID: The selected location is invalid or inactive.");
            }

            var hasLocationAssignments = await _context.ServiceLocations
                .IgnoreQueryFilters()
                .AnyAsync(sl => sl.ServiceId == service.Id, cancellationToken);

            if (hasLocationAssignments)
            {
                var isLocationAssigned = await _context.ServiceLocations
                    .IgnoreQueryFilters()
                    .AnyAsync(sl => sl.ServiceId == service.Id && sl.LocationId == locationId.Value && sl.IsAvailable, cancellationToken);

                if (!isLocationAssigned)
                {
                    throw new ValidationException("LOCATION_UNAVAILABLE: This service is not available at the selected location.");
                }
            }
        }

        // 4. Past Appointment Check
        if (!allowPast)
        {
            var now = DateTimeOffset.UtcNow;
            if (startUtc < now.AddMinutes(-5))
            {
                throw new ValidationException("APPOINTMENT_IN_PAST: Appointments cannot be scheduled in the past unless explicitly marked as a historical record.");
            }
        }

        // 5. Business Operating Hours & Closed Day Check
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        var tzId = tenant?.TimeZoneId ?? "Asia/Kolkata";
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tzId) ?? DateTimeZoneProviders.Tzdb["Asia/Kolkata"];

        var startInstant = Instant.FromDateTimeOffset(startUtc);
        var endInstant = Instant.FromDateTimeOffset(endUtc);

        var startZoned = startInstant.InZone(zone);
        var endZoned = endInstant.InZone(zone);

        var localDate = startZoned.Date;
        var dayOfWeek = (DayOfWeek)(((int)startZoned.DayOfWeek) % 7);
        var startLocalTime = startZoned.TimeOfDay;
        var endLocalTime = endZoned.TimeOfDay;

        if (!string.IsNullOrWhiteSpace(tenant?.BusinessHoursJson))
        {
            try
            {
                var businessHours = JsonSerializer.Deserialize<List<BusinessDayHours>>(tenant.BusinessHoursJson, JsonOptions);
                var dayConfig = businessHours?.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
                if (dayConfig != null)
                {
                    if (!dayConfig.IsOpen)
                    {
                        throw new ValidationException($"OUTSIDE_BUSINESS_HOURS: The business is closed on {dayOfWeek}.");
                    }

                    if (dayConfig.OpenTime.HasValue && dayConfig.CloseTime.HasValue)
                    {
                        var openNoda = LocalTime.FromTicksSinceMidnight(dayConfig.OpenTime.Value.ToTimeSpan().Ticks);
                        var closeNoda = LocalTime.FromTicksSinceMidnight(dayConfig.CloseTime.Value.ToTimeSpan().Ticks);

                        if (startLocalTime < openNoda || endLocalTime > closeNoda)
                        {
                            throw new ValidationException($"OUTSIDE_BUSINESS_HOURS: Appointment time ({startLocalTime:HH:mm} - {endLocalTime:HH:mm}) is outside business operating hours ({openNoda:HH:mm} - {closeNoda:HH:mm}).");
                        }
                    }

                    if (dayConfig.Breaks != null && dayConfig.Breaks.Any())
                    {
                        foreach (var b in dayConfig.Breaks)
                        {
                            var bStart = LocalTime.FromTicksSinceMidnight(b.StartTime.ToTimeSpan().Ticks);
                            var bEnd = LocalTime.FromTicksSinceMidnight(b.EndTime.ToTimeSpan().Ticks);
                            if (startLocalTime < bEnd && endLocalTime > bStart)
                            {
                                throw new ValidationException($"OUTSIDE_BUSINESS_HOURS: Appointment time conflicts with business break ({bStart:HH:mm} - {bEnd:HH:mm}).");
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fallback if JSON is malformed
            }
        }

        // 6. Staff Working Hours & Break Windows
        var workingHours = await _context.WorkingHours
            .IgnoreQueryFilters()
            .Where(w => w.StaffId == staffId && w.DayOfWeek == dayOfWeek)
            .ToListAsync(cancellationToken);

        if (!workingHours.Any())
        {
            throw new ValidationException($"STAFF_UNAVAILABLE: The staff member has no working hours scheduled on {dayOfWeek}.");
        }

        bool fitsWithinWorkingWindow = false;
        foreach (var wh in workingHours)
        {
            var whStart = LocalTime.FromTicksSinceMidnight(wh.StartTime.ToTimeSpan().Ticks);
            var whEnd = LocalTime.FromTicksSinceMidnight(wh.EndTime.ToTimeSpan().Ticks);

            if (startLocalTime >= whStart && endLocalTime <= whEnd)
            {
                fitsWithinWorkingWindow = true;
                break;
            }
        }

        if (!fitsWithinWorkingWindow)
        {
            throw new ValidationException("STAFF_UNAVAILABLE: The requested appointment falls outside the staff member's working hours or conflicts with a scheduled break.");
        }

        // 7. Staff Time-Off Overlap
        var hasTimeOff = await _context.TimeOffs
            .IgnoreQueryFilters()
            .AnyAsync(t => t.StaffId == staffId && t.StartUtc < endUtc && t.EndUtc > startUtc, cancellationToken);

        if (hasTimeOff)
        {
            throw new ValidationException("STAFF_TIME_OFF: The selected staff member has approved time-off during this window.");
        }

        // 8. Staff Appointment Overlap (Double-Booking Prevention)
        var bookingsQuery = _context.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.StaffId == staffId &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Pending) &&
                        b.StartUtc < endUtc &&
                        b.EndUtc > startUtc);

        if (excludeBookingId.HasValue && excludeBookingId.Value != Guid.Empty)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Id != excludeBookingId.Value);
        }

        var hasBookingOverlap = await bookingsQuery.AnyAsync(cancellationToken);
        if (hasBookingOverlap)
        {
            throw new ValidationException("SCHEDULING_CONFLICT: The selected staff member already has an appointment during this time window.");
        }

        // 9. Resource Availability (if required by service)
        if (service.RequiresResource && service.RequiredResourceId.HasValue)
        {
            var resId = service.RequiredResourceId.Value;
            var resource = await _context.Resources
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == resId && r.TenantId == tenantId && r.IsActive, cancellationToken);

            if (resource == null)
            {
                throw new ValidationException("RESOURCE_UNAVAILABLE: The required service resource is unavailable or inactive.");
            }

            var resConflictQuery = _context.Bookings
                .IgnoreQueryFilters()
                .Where(b => b.TenantId == tenantId &&
                            b.ResourceId == resId &&
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Pending) &&
                            b.StartUtc < endUtc &&
                            b.EndUtc > startUtc);

            if (excludeBookingId.HasValue && excludeBookingId.Value != Guid.Empty)
            {
                resConflictQuery = resConflictQuery.Where(b => b.Id != excludeBookingId.Value);
            }

            var hasResConflict = await resConflictQuery.AnyAsync(cancellationToken);
            if (hasResConflict)
            {
                throw new ValidationException("RESOURCE_CONFLICT: The required resource is already reserved during this time window.");
            }
        }
    }
}
