namespace Bookline.Application.Bookings.Queries;

using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record CalendarBookingDto(
    Guid Id,
    Guid StaffId,
    string StaffName,
    Guid ServiceId,
    string ServiceName,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    BookingStatus Status);

public record GetCalendarBookingsQuery(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? StaffId = null) : IRequest<IReadOnlyList<CalendarBookingDto>>;

public class GetCalendarBookingsQueryValidator : AbstractValidator<GetCalendarBookingsQuery>
{
    public GetCalendarBookingsQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThan(query => query.FromUtc)
            .Must((query, toUtc) => toUtc - query.FromUtc <= TimeSpan.FromDays(31));
    }
}

public class GetCalendarBookingsQueryHandler
    : IRequestHandler<GetCalendarBookingsQuery, IReadOnlyList<CalendarBookingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCalendarBookingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CalendarBookingDto>> Handle(
        GetCalendarBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.StartUtc < request.ToUtc && booking.EndUtc > request.FromUtc);

        if (request.StaffId.HasValue)
        {
            query = query.Where(booking => booking.StaffId == request.StaffId.Value);
        }

        return await query
            .OrderBy(booking => booking.StartUtc)
            .Select(booking => new CalendarBookingDto(
                booking.Id,
                booking.StaffId,
                booking.Staff.Name,
                booking.ServiceId,
                booking.Service.Name,
                booking.CustomerId,
                booking.Customer.FirstName + " " + booking.Customer.LastName,
                booking.StartUtc,
                booking.EndUtc,
                booking.Status))
            .ToListAsync(cancellationToken);
    }
}