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
    BookingStatus Status,
    string? BookingReference = null,
    string? CustomerPhone = null,
    string? CustomerEmail = null,
    Guid? LocationId = null,
    string? LocationName = null,
    string Source = "Online",
    decimal Price = 0.00m,
    string Currency = "INR",
    string? CustomerNotes = null,
    string? InternalNotes = null,
    string? TimeZoneId = "Asia/Kolkata",
    DateTimeOffset? CreatedAtUtc = null);

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
            .Include(b => b.Staff)
            .Include(b => b.Service)
            .Include(b => b.Customer)
            .Where(booking => booking.StartUtc < request.ToUtc && booking.EndUtc > request.FromUtc);

        if (request.StaffId.HasValue)
        {
            query = query.Where(booking => booking.StaffId == request.StaffId.Value);
        }

        var items = await query
            .OrderBy(booking => booking.StartUtc)
            .ToListAsync(cancellationToken);

        var locationIds = items.Where(b => b.LocationId.HasValue).Select(b => b.LocationId!.Value).Distinct().ToList();
        var locations = locationIds.Count > 0
            ? await _context.Locations.AsNoTracking().Where(l => locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken)
            : new Dictionary<Guid, Location>();

        return items.Select(booking =>
        {
            Location? loc = null;
            if (booking.LocationId.HasValue) locations.TryGetValue(booking.LocationId.Value, out loc);

            var sName = !string.IsNullOrEmpty(booking.ServiceNameSnapshot) 
                ? booking.ServiceNameSnapshot 
                : (booking.Service != null ? booking.Service.Name : "Service");

            var price = booking.PriceSnapshot > 0 ? booking.PriceSnapshot : booking.TotalPrice;

            return new CalendarBookingDto(
                booking.Id,
                booking.StaffId,
                booking.Staff != null ? booking.Staff.Name : "Staff",
                booking.ServiceId,
                sName,
                booking.CustomerId,
                booking.Customer != null ? (booking.Customer.FirstName + " " + booking.Customer.LastName).Trim() : "Customer",
                booking.StartUtc,
                booking.EndUtc,
                booking.Status,
                booking.BookingReference,
                booking.Customer?.Phone,
                booking.Customer?.Email,
                booking.LocationId,
                loc?.Name,
                booking.Source.ToString(),
                price,
                booking.Currency,
                booking.CustomerNotes,
                booking.InternalNotes,
                booking.TimeZoneId ?? "Asia/Kolkata",
                booking.CreatedAtUtc
            );
        }).ToList();
    }
}