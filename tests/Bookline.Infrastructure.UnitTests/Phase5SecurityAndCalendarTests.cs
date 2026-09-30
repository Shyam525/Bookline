namespace Bookline.Infrastructure.UnitTests;

using Bookline.Domain.Entities;
using Bookline.Infrastructure.Services;
using Ical.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class Phase5SecurityAndCalendarTests
{
    private readonly BookingActionTokenService _tokenService;
    private readonly CalendarService _calendarService = new();

    public Phase5SecurityAndCalendarTests()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        _tokenService = new BookingActionTokenService(provider);
    }

    [Fact]
    public void Token_ValidToken_ShouldDecryptBookingIdAndAction()
    {
        var bookingId = Guid.NewGuid();
        var action = "reschedule";

        var token = _tokenService.GenerateToken(bookingId, action, TimeSpan.FromHours(1));
        var isValid = _tokenService.TryValidateToken(token, out var extractedBookingId, out var extractedAction);

        Assert.True(isValid);
        Assert.Equal(bookingId, extractedBookingId);
        Assert.Equal(action, extractedAction);
    }

    [Fact]
    public void Token_TamperedToken_ShouldFailValidation()
    {
        var bookingId = Guid.NewGuid();
        var token = _tokenService.GenerateToken(bookingId, "cancel", TimeSpan.FromHours(1));

        var tamperedToken = token + "tampered";
        var isValid = _tokenService.TryValidateToken(tamperedToken, out _, out _);

        Assert.False(isValid);
    }

    [Fact]
    public void Token_ExpiredToken_ShouldFailValidation()
    {
        var bookingId = Guid.NewGuid();
        var expiredToken = _tokenService.GenerateToken(bookingId, "reschedule", TimeSpan.FromMilliseconds(-100));

        var isValid = _tokenService.TryValidateToken(expiredToken, out _, out _);

        Assert.False(isValid);
    }

    [Fact]
    public void IcsCalendar_ParsesBackToSameStartAndEnd()
    {
        var tenant = new Tenant { Name = "Style Salon", Slug = "style-salon" };
        var staff = new Staff { Name = "John Doe" };
        var service = new Service { Name = "Haircut", DurationMinutes = 45 };
        var startUtc = new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero);
        var endUtc = startUtc.AddMinutes(45);

        var booking = new Booking(tenant.Id, staff.Id, service.Id, Guid.NewGuid(), startUtc, endUtc);

        var icsBytes = _calendarService.GenerateIcsAttachment(booking, service, staff, tenant);
        Assert.NotNull(icsBytes);
        Assert.NotEmpty(icsBytes);

        using var ms = new MemoryStream(icsBytes);
        var calendar = Calendar.Load(ms);

        Assert.NotNull(calendar);
        Assert.Single(calendar.Events);

        var evt = calendar.Events[0];
        Assert.Equal(startUtc.UtcDateTime, evt.Start.AsUtc);
        Assert.Equal(endUtc.UtcDateTime, evt.End.AsUtc);
        Assert.Contains(service.Name, evt.Summary);
    }
}
