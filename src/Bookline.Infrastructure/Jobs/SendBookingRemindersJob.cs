namespace Bookline.Infrastructure.Jobs;

using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class SendBookingRemindersJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IBookingActionTokenService _tokenService;

    public SendBookingRemindersJob(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IBookingActionTokenService tokenService)
    {
        _context = context;
        _emailSender = emailSender;
        _tokenService = tokenService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddHours(23.5);
        var windowEnd = now.AddHours(24.5);

        var upcomingBookings = await _context.Bookings
            .IgnoreQueryFilters()
            .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
                        && b.ReminderSentAtUtc == null
                        && b.StartUtc >= windowStart
                        && b.StartUtc <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var booking in upcomingBookings)
        {
            var staff = await _context.Staff.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == booking.StaffId, cancellationToken);
            var service = await _context.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == booking.CustomerId, cancellationToken);
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == booking.TenantId, cancellationToken);

            if (staff != null && service != null && customer != null && tenant != null)
            {
                var rescheduleToken = _tokenService.GenerateToken(booking.Id, "reschedule", TimeSpan.FromDays(7));
                var cancelToken = _tokenService.GenerateToken(booking.Id, "cancel", TimeSpan.FromDays(7));

                await _emailSender.SendBookingConfirmationEmailAsync(
                    booking, service, staff, customer, tenant, rescheduleToken, cancelToken, cancellationToken);

                booking.MarkReminderSent(now);
            }
        }

        if (upcomingBookings.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
