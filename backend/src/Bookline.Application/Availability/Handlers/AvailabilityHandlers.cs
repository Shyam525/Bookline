using Bookline.Application.Availability.Commands;
using Bookline.Application.Availability.DTOs;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Staff.Commands;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Bookline.Application.Availability.Handlers;

public class AvailabilityHandlers :
    IRequestHandler<GetAvailabilitySlotsQuery, List<TimeSlotDto>>,
    IRequestHandler<GetStaffTimeOffQuery, List<TimeOffDto>>,
    IRequestHandler<DeleteTimeOffCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISlotEngine _slotEngine;

    public AvailabilityHandlers(IApplicationDbContext context, ISlotEngine slotEngine)
    {
        _context = context;
        _slotEngine = slotEngine;
    }

    public async Task<List<TimeSlotDto>> Handle(GetAvailabilitySlotsQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive && !s.IsArchived, cancellationToken);

        if (service == null) throw new NotFoundException("Service", request.ServiceId);

        var parseResult = LocalDatePattern.Iso.Parse(request.Date);
        if (!parseResult.Success)
        {
            throw new ValidationException("Invalid date format. Use YYYY-MM-DD.");
        }
        var localDate = parseResult.Value;

        var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(request.Timezone) ?? DateTimeZone.Utc;

        // Query eligible staff
        var staffQuery = _context.Staff
            .AsNoTracking()
            .Include(s => s.StaffServices)
            .Include(s => s.WorkingHours)
            .Where(s => s.IsActive && !s.IsArchived);

        if (request.StaffId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.Id == request.StaffId.Value);
        }
        else
        {
            staffQuery = staffQuery.Where(s => s.StaffServices.Any(ss => ss.ServiceId == service.Id));
        }

        var staffList = await staffQuery.ToListAsync(cancellationToken);
        if (!staffList.Any()) return new List<TimeSlotDto>();

        var resultSlots = new List<TimeSlotDto>();

        var svcInfo = new ServiceInfo(
            Duration.FromMinutes(service.DurationMinutes),
            Duration.FromMinutes(service.BufferBeforeMinutes + service.BufferAfterMinutes)
        );

        var targetDayOfWeek = (DayOfWeek)(((int)localDate.DayOfWeek) % 7);
        var nowInstant = SystemClock.Instance.GetCurrentInstant();
        var slotStep = Duration.FromMinutes(15);
        var minNotice = Duration.FromHours(1);

        foreach (var staff in staffList)
        {
            var workingHour = staff.WorkingHours.FirstOrDefault(wh => wh.DayOfWeek == targetDayOfWeek);
            if (workingHour == null) continue;

            var staffSchedule = new StaffSchedule(new List<WorkingWindow>
            {
                new WorkingWindow(
                    new LocalTime(workingHour.StartTime.Hour, workingHour.StartTime.Minute),
                    new LocalTime(workingHour.EndTime.Hour, workingHour.EndTime.Minute)
                )
            });

            // Fetch staff bookings for local date interval
            var startOfDayUtc = localDate.AtStartOfDayInZone(tz).ToInstant().ToDateTimeOffset();
            var endOfDayUtc = localDate.PlusDays(1).AtStartOfDayInZone(tz).ToInstant().ToDateTimeOffset();

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.StaffId == staff.Id &&
                            b.Status != BookingStatus.Cancelled &&
                            b.StartUtc < endOfDayUtc &&
                            b.EndUtc > startOfDayUtc)
                .ToListAsync(cancellationToken);

            var bookingIntervals = bookings.Select(b => new Interval(
                Instant.FromDateTimeOffset(b.StartUtc),
                Instant.FromDateTimeOffset(b.EndUtc)
            )).ToList();

            var timeOffs = await _context.TimeOffs
                .AsNoTracking()
                .Where(to => to.StaffId == staff.Id &&
                             to.StartUtc < endOfDayUtc &&
                             to.EndUtc > startOfDayUtc)
                .ToListAsync(cancellationToken);

            var timeOffIntervals = timeOffs.Select(to => new Interval(
                Instant.FromDateTimeOffset(to.StartUtc),
                Instant.FromDateTimeOffset(to.EndUtc)
            )).ToList();

            var computedSlots = _slotEngine.Compute(
                svcInfo,
                staffSchedule,
                bookingIntervals,
                timeOffIntervals,
                localDate,
                tz,
                nowInstant,
                slotStep,
                minNotice
            );

            foreach (var slot in computedSlots)
            {
                var startZonedDateTime = slot.Start.InZone(tz);
                var endZonedDateTime = slot.End.InZone(tz);

                var display = $"{startZonedDateTime.ToString("hh:mm tt", null)} - {endZonedDateTime.ToString("hh:mm tt", null)}";

                resultSlots.Add(new TimeSlotDto(
                    StartIso: slot.Start.ToString(),
                    EndIso: slot.End.ToString(),
                    DisplayTime: display,
                    IsAvailable: true,
                    StaffId: staff.Id,
                    StaffName: staff.Name,
                    Date: startZonedDateTime.Date.ToString("yyyy-MM-dd", null),
                    LocalTime: startZonedDateTime.TimeOfDay.ToString("HH:mm", null),
                    Instant: slot.Start.ToString(),
                    Timezone: tz.Id,
                    DurationMinutes: (int)svcInfo.Duration.TotalMinutes
                ));
            }
        }

        return resultSlots.OrderBy(s => s.StartIso).ToList();
    }

    public async Task<List<TimeOffDto>> Handle(GetStaffTimeOffQuery request, CancellationToken cancellationToken)
    {
        var timeOffs = await _context.TimeOffs
            .AsNoTracking()
            .Where(to => to.StaffId == request.StaffId)
            .OrderByDescending(to => to.StartUtc)
            .ToListAsync(cancellationToken);

        return timeOffs.Select(to => new TimeOffDto(
            to.Id,
            to.StaffId,
            to.StartUtc,
            to.EndUtc,
            to.Reason
        )).ToList();
    }

    public async Task<bool> Handle(DeleteTimeOffCommand command, CancellationToken cancellationToken)
    {
        var timeOff = await _context.TimeOffs
            .FirstOrDefaultAsync(to => to.Id == command.TimeOffId && to.StaffId == command.StaffId, cancellationToken);

        if (timeOff == null) throw new NotFoundException("TimeOff", command.TimeOffId);

        _context.TimeOffs.Remove(timeOff);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
