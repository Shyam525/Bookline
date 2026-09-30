namespace Bookline.Infrastructure.Services;

using System.Text;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

public class CalendarService : ICalendarService
{
    public byte[] GenerateIcsAttachment(Booking booking, Service service, Staff staff, Tenant tenant)
    {
        var calendar = new Calendar();
        calendar.Method = "REQUEST";

        var evt = new CalendarEvent
        {
            Uid = $"{booking.Id}@bookline.app",
            Summary = $"{service.Name} with {staff.Name}",
            Description = $"Booking confirmation for {service.Name} at {tenant.Name}.",
            Start = new CalDateTime(booking.StartUtc.UtcDateTime, "UTC"),
            End = new CalDateTime(booking.EndUtc.UtcDateTime, "UTC"),
            Status = EventStatus.Confirmed,
            Created = new CalDateTime(DateTime.UtcNow, "UTC")
        };

        calendar.Events.Add(evt);

        var serializer = new CalendarSerializer();
        var stringOutput = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(stringOutput);
    }
}
