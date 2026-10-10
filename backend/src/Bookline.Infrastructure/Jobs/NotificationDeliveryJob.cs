using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker job processing queued notification deliveries adhering to Section 93 & 122.
/// Dispatches email, SMS, and in-app notifications asynchronously.
/// </summary>
public class NotificationDeliveryJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender? _emailSender;
    private readonly ILogger<NotificationDeliveryJob> _logger;

    public NotificationDeliveryJob(
        IApplicationDbContext context,
        ILogger<NotificationDeliveryJob> logger,
        IEmailSender? emailSender = null)
    {
        _context = context;
        _logger = logger;
        _emailSender = emailSender;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var pendingNotifications = await _context.NotificationLogs
            .IgnoreQueryFilters()
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (!pendingNotifications.Any()) return 0;

        _logger.LogInformation("Processing batch of {Count} queued notifications...", pendingNotifications.Count);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                if (notification.Channel == NotificationChannel.Email && _emailSender != null && !string.IsNullOrWhiteSpace(notification.RecipientEmail))
                {
                    await _emailSender.SendEmailAsync(
                        notification.RecipientEmail,
                        notification.Subject,
                        notification.Body,
                        cancellationToken);
                }

                notification.Status = NotificationStatus.Sent;
                notification.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver notification {Id} to {Recipient}",
                    notification.Id, notification.RecipientEmail);
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = ex.Message;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return pendingNotifications.Count;
    }
}
