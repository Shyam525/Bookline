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
    string TimeZoneId
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

    public GetPublicAvailabilityQueryHandler(IApplicationDbContext context, ISlotEngine slotEngine)
    {
        _context = context;
        _slotEngine = slotEngine;
    }

    public async Task<IReadOnlyList<Slot>> Handle(GetPublicAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), request.ServiceId);
        }

        var staff = await _context.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
        {
            throw new NotFoundException(nameof(Staff), request.StaffId);
        }

        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(request.TimeZoneId)
            ?? DateTimeZoneProviders.Tzdb.GetZoneOrNull(staff.TimeZoneId)
            ?? DateTimeZoneProviders.Tzdb["UTC"];


        var workingHours = await _context.WorkingHours
            .AsNoTracking()
            .Where(w => w.StaffId == request.StaffId)
            .ToListAsync(cancellationToken);

        var existingBookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.StaffId == request.StaffId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync(cancellationToken);

        var timeOffs = await _context.TimeOffs
            .AsNoTracking()
            .Where(t => t.StaffId == request.StaffId)
            .ToListAsync(cancellationToken);

        var dayHours = workingHours.Where(w => (int)w.DayOfWeek == (int)request.Date.DayOfWeek).ToList();
        var windows = dayHours.Select(w => new WorkingWindow(
            LocalTime.FromTicksSinceMidnight(w.StartTime.Ticks),
            LocalTime.FromTicksSinceMidnight(w.EndTime.Ticks)
        )).ToList();

        var serviceInfo = new ServiceInfo(
            Duration.FromMinutes(service.DurationMinutes),
            Duration.FromMinutes(service.BufferMinutes));

        var schedule = new StaffSchedule(windows);

        var bookingIntervals = existingBookings.Select(b => new Interval(
            Instant.FromDateTimeOffset(b.StartUtc),
            Instant.FromDateTimeOffset(b.EndUtc)
        )).ToList();

        var timeOffIntervals = timeOffs.Select(t => new Interval(
            Instant.FromDateTimeOffset(t.StartUtc),
            Instant.FromDateTimeOffset(t.EndUtc)
        )).ToList();

        return _slotEngine.Compute(
            serviceInfo,
            schedule,
            bookingIntervals,
            timeOffIntervals,
            request.Date,
            zone,
            SystemClock.Instance.GetCurrentInstant(),
            Duration.FromMinutes(15),
            Duration.FromMinutes(30));
    }
}
