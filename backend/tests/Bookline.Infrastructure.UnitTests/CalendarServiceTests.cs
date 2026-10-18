namespace Bookline.Infrastructure.UnitTests;

using System.IO;
using System.Text;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Services;
using Ical.Net;
using NodaTime;
using Xunit;

public class CalendarServiceTests
{
    private sealed class TestClock(Instant current) : IClock
    {
        public Instant GetCurrentInstant() => current;
    }
    [Fact]
    public void GenerateIcsAttachment_WithDeterministicClock_SetsCreatedTimestampAccurately()
    {
        var fixedInstant = Instant.FromUtc(2026, 10, 19, 12, 0, 0);
        var clock = new TestClock(fixedInstant);
        var calendarService = new CalendarService(clock);

        var tenant = new Tenant { Name = "Glow Studio", TimeZoneId = "UTC" };
        var staff = new Staff { Name = "Alice" };
        var service = new Service { Name = "Manicure", DurationMinutes = 60 };
        var startUtc = new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(60);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, Guid.NewGuid(), startUtc, endUtc);

        var icsBytes = calendarService.GenerateIcsAttachment(booking, service, staff, tenant);
        using var ms = new MemoryStream(icsBytes);
        var calendar = Calendar.Load(ms);

        Assert.NotNull(calendar);
        var evt = Assert.Single(calendar.Events);
        Assert.Equal(fixedInstant.ToDateTimeUtc(), evt.Created.AsUtc);
        Assert.Equal(fixedInstant.ToDateTimeUtc(), evt.LastModified.AsUtc);
    }

    [Fact]
    public void GenerateIcsAttachment_WithLocationTimezone_IncludesZonedTimesInDescription()
    {
        var fixedInstant = Instant.FromUtc(2026, 10, 19, 10, 0, 0);
        var clock = new TestClock(fixedInstant);
        var calendarService = new CalendarService(clock);

        // 14:00 UTC = 10:00 EDT (America/New_York is UTC-4 in October)
        var tenant = new Tenant 
        { 
            Name = "Manhattan Spa", 
            TimeZoneId = "America/New_York",
            Address = "123 5th Ave",
            City = "New York"
        };
        var staff = new Staff { Name = "Bob" };
        var service = new Service { Name = "Deep Tissue Massage", DurationMinutes = 60 };
        var startUtc = new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(60);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, Guid.NewGuid(), startUtc, endUtc);

        var icsBytes = calendarService.GenerateIcsAttachment(booking, service, staff, tenant);
        var icsString = Encoding.UTF8.GetString(icsBytes);

        Assert.Contains("America/New_York", icsString);
        Assert.Contains("10:00", icsString); // local start time
        Assert.Contains("11:00", icsString); // local end time
        Assert.Contains("14:00", icsString); // UTC start time
        Assert.Contains("123 5th Ave", icsString);
    }

    [Fact]
    public void GenerateIcsAttachment_WithIndianTimezone_FormatsISTAccurately()
    {
        var calendarService = new CalendarService();
        var tenant = new Tenant { Name = "Ahmedabad Cuts", TimeZoneId = "Asia/Kolkata" };
        var staff = new Staff { Name = "Raj" };
        var service = new Service { Name = "Beard Trim", DurationMinutes = 30 };
        // 08:30 UTC = 14:00 IST (+5:30)
        var startUtc = new DateTimeOffset(2026, 10, 20, 8, 30, 0, TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(30);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, Guid.NewGuid(), startUtc, endUtc);

        var icsBytes = calendarService.GenerateIcsAttachment(booking, service, staff, tenant);
        var icsString = Encoding.UTF8.GetString(icsBytes);

        Assert.Contains("Asia/Kolkata", icsString);
        Assert.Contains("14:00", icsString); // local start time in IST
    }

    [Fact]
    public void GenerateIcsAttachment_WithInvalidTimezone_FallsBackGracefullyToUtc()
    {
        var calendarService = new CalendarService();
        var tenant = new Tenant { Name = "Mystery Salon", TimeZoneId = "Invalid/Zone/Mars" };
        var staff = new Staff { Name = "Sarah" };
        var service = new Service { Name = "Consultation", DurationMinutes = 30 };
        var startUtc = new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(30);
        var booking = new Booking(tenant.Id, staff.Id, service.Id, Guid.NewGuid(), startUtc, endUtc);

        var icsBytes = calendarService.GenerateIcsAttachment(booking, service, staff, tenant);
        Assert.NotNull(icsBytes);
        Assert.NotEmpty(icsBytes);

        var icsString = Encoding.UTF8.GetString(icsBytes);
        Assert.Contains("UTC", icsString);
    }
}
