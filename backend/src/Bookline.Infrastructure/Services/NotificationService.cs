namespace Bookline.Infrastructure.Services;

using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IEmailSender emailSender,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        Guid tenantId,
        NotificationType type,
        NotificationChannel channel,
        string recipientEmail,
        string? recipientPhone,
        string subject,
        string body,
        Guid? bookingId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        var isEmailEnabled = setting?.EmailNotificationsEnabled ?? true;
        var isSmsEnabled = setting?.SmsNotificationsEnabled ?? false;

        if (channel == NotificationChannel.Email && !isEmailEnabled)
        {
            _logger.LogInformation("Email notifications disabled for tenant {TenantId}. Skipping message.", tenantId);
            return;
        }

        if (channel == NotificationChannel.SMS && !isSmsEnabled)
        {
            _logger.LogInformation("SMS notifications disabled for tenant {TenantId}. Skipping message.", tenantId);
            return;
        }

        var log = new NotificationLog
        {
            TenantId = tenantId,
            BookingId = bookingId,
            CustomerId = customerId,
            RecipientEmail = recipientEmail,
            RecipientPhone = recipientPhone,
            NotificationType = type,
            Channel = channel,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            if (channel == NotificationChannel.Email)
            {
                await _emailSender.SendEmailAsync(recipientEmail, subject, body, cancellationToken);
            }
            else if (channel == NotificationChannel.SMS)
            {
                // SMS gateway log dispatch simulation
                _logger.LogInformation("[SMS GATEWAY] Sent SMS to {Phone}: {Body}", recipientPhone ?? recipientEmail, body);
            }

            log.Status = NotificationStatus.Sent;
            log.SentAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver notification {LogId} to {Recipient}", log.Id, recipientEmail);
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessUpcomingRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var window24hStart = now.AddHours(23).AddMinutes(30);
        var window24hEnd = now.AddHours(24).AddMinutes(30);

        var upcomingBookings = await _context.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.Status == BookingStatus.Confirmed
                        && b.StartUtc >= window24hStart
                        && b.StartUtc <= window24hEnd)
            .ToListAsync(cancellationToken);

        foreach (var booking in upcomingBookings)
        {
            // Check if reminder was already sent
            var exists = await _context.NotificationLogs
                .IgnoreQueryFilters()
                .AnyAsync(n => n.BookingId == booking.Id && n.NotificationType == NotificationType.Reminder24h, cancellationToken);

            if (exists) continue;

            var customer = await _context.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == booking.CustomerId, cancellationToken);

            if (customer == null || string.IsNullOrWhiteSpace(customer.Email)) continue;

            var subject = "Reminder: Upcoming Appointment Tomorrow";
            var body = $"Hello {customer.FirstName}, this is a reminder for your upcoming appointment on {booking.StartUtc:F} UTC.";

            await SendNotificationAsync(
                tenantId: booking.TenantId,
                type: NotificationType.Reminder24h,
                channel: NotificationChannel.Email,
                recipientEmail: customer.Email,
                recipientPhone: customer.Phone,
                subject: subject,
                body: body,
                bookingId: booking.Id,
                customerId: customer.Id,
                cancellationToken: cancellationToken
            );
        }
    }
}
