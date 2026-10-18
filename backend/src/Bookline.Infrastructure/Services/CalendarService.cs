namespace Bookline.Infrastructure.Services;

using System.Text;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NodaTime;

public class CalendarService : ICalendarService
{
    private readonly IClock _clock;

    public CalendarService(IClock? clock = null)
    {
        _clock = clock ?? SystemClock.Instance;
    }

    public byte[] GenerateIcsAttachment(Booking booking, Service service, Staff staff, Tenant tenant)
    {
        var calendar = new Calendar();
        calendar.Method = "REQUEST";

        var createdInstant = _clock.GetCurrentInstant();
        var createdUtc = createdInstant.ToDateTimeUtc();

        // Location timezone resolution via Tzdb
        var locationTzId = string.IsNullOrWhiteSpace(tenant.TimeZoneId) ? "UTC" : tenant.TimeZoneId;
        var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(locationTzId) ?? DateTimeZone.Utc;

        var startInstant = Instant.FromDateTimeOffset(booking.StartUtc);
        var endInstant = Instant.FromDateTimeOffset(booking.EndUtc);

        var startZoned = startInstant.InZone(tz);
        var endZoned = endInstant.InZone(tz);

        var locationStr = string.IsNullOrWhiteSpace(tenant.Address) 
            ? tenant.City 
            : $"{tenant.Address}, {tenant.City}";

        var description = $"Booking confirmation for {service.Name} with {staff.Name} at {tenant.Name}.\n" +
                          $"Location Time ({tz.Id}): {startZoned:yyyy-MM-dd HH:mm} - {endZoned:HH:mm}\n" +
                          $"UTC: {startInstant:yyyy-MM-dd HH:mm}Z - {endInstant:HH:mm}Z\n" +
                          $"Reference: {booking.BookingReference}";

        var evt = new CalendarEvent
        {
            Uid = $"{booking.Id}@bookline.app",
            Summary = $"{service.Name} with {staff.Name} - {tenant.Name}",
            Description = description,
            Start = new CalDateTime(booking.StartUtc.UtcDateTime, "UTC"),
            End = new CalDateTime(booking.EndUtc.UtcDateTime, "UTC"),
            Status = EventStatus.Confirmed,
            Created = new CalDateTime(createdUtc, "UTC"),
            LastModified = new CalDateTime(createdUtc, "UTC"),
            Location = locationStr
        };

        calendar.Events.Add(evt);

        var serializer = new CalendarSerializer();
        var stringOutput = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(stringOutput);
    }
}
