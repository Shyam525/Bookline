using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Constants;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bookline.Infrastructure.Jobs;

/// <summary>
/// Background worker processing transactional Outbox events and scheduled timezone-aware reminders (Sections 90, 91, 92).
/// Follows strict transactional pattern: business change + audit + outbox committed, then worker dispatches notifications.
/// </summary>
public class OutboxAndReminderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxAndReminderWorker> _logger;
    private DateTime _lastReminderRunUtc = DateTime.MinValue;
    private DateTime _lastCleanupRunUtc = DateTime.MinValue;
    private DateTime _lastReportRunUtc = DateTime.MinValue;

    public OutboxAndReminderWorker(IServiceProvider serviceProvider, ILogger<OutboxAndReminderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox and 7-Domain Background Worker Suite started (Section 122).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                tenantContext?.EnableSystemMode();
                var dbContext = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();
                var emailSender = scope.ServiceProvider.GetService<IEmailSender>();

                // 1. Outbox events (Section 91 & 122)
                await ProcessOutboxBatchAsync(dbContext, emailSender, stoppingToken);

                // 2. Expired holds cleanup (Section 58 & 122)
                var holdsLogger = scope.ServiceProvider.GetService<ILogger<ExpiredHoldsCleanupJob>>()
                    ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExpiredHoldsCleanupJob>();
                var holdsJob = new ExpiredHoldsCleanupJob(dbContext, holdsLogger);
                await holdsJob.ExecuteAsync(stoppingToken);

                // 3. Timezone-Aware 24h & 2h Reminders (Section 92 & 122, every 60s)
                if (DateTime.UtcNow - _lastReminderRunUtc >= TimeSpan.FromSeconds(60))
                {
                    await ProcessRemindersAsync(dbContext, stoppingToken);
                    _lastReminderRunUtc = DateTime.UtcNow;
                }

                // 4. Notifications delivery (Section 93 & 122)
                var notifLogger = scope.ServiceProvider.GetService<ILogger<NotificationDeliveryJob>>()
                    ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<NotificationDeliveryJob>();
                var notifJob = new NotificationDeliveryJob(dbContext, notifLogger, emailSender);
                await notifJob.ExecuteAsync(stoppingToken);

                // 5 & 7. Invitations & Inventory cleanup (Section 119 & 122, every 5 mins)
                if (DateTime.UtcNow - _lastCleanupRunUtc >= TimeSpan.FromMinutes(5))
                {
                    var invLogger = scope.ServiceProvider.GetService<ILogger<InvitationCleanupJob>>()
                        ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<InvitationCleanupJob>();
                    var invJob = new InvitationCleanupJob(dbContext, invLogger);
                    await invJob.ExecuteAsync(stoppingToken);

                    var inventoryLogger = scope.ServiceProvider.GetService<ILogger<InventoryCleanupJob>>()
                        ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<InventoryCleanupJob>();
                    var inventoryJob = new InventoryCleanupJob(dbContext, inventoryLogger);
                    await inventoryJob.ExecuteAsync(stoppingToken);

                    _lastCleanupRunUtc = DateTime.UtcNow;
                }

                // 6. Report aggregation jobs (Section 122, every 1 hour)
                if (DateTime.UtcNow - _lastReportRunUtc >= TimeSpan.FromHours(1))
                {
                    var reportLogger = scope.ServiceProvider.GetService<ILogger<ReportAggregationJob>>()
                        ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ReportAggregationJob>();
                    var reportJob = new ReportAggregationJob(dbContext, reportLogger);
                    await reportJob.ExecuteAsync(stoppingToken);
                    _lastReportRunUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Section 122 Background Worker cycle.");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(BooklineDbContext dbContext, IEmailSender? emailSender, CancellationToken cancellationToken)
    {
        var pendingMessages = await dbContext.OutboxMessages.IgnoreQueryFilters()
            .Where(m => m.Status == "Pending" && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any()) return;

        foreach (var msg in pendingMessages)
        {
            try
            {
                _logger.LogInformation("Dispatching Outbox Event [{Id}] ({EventType}) for Tenant {TenantId}",
                    msg.Id, msg.EventType, msg.TenantId);

                await DispatchOutboxNotificationAsync(dbContext, msg, emailSender, cancellationToken);

                msg.Status = "Processed";
                msg.ProcessedAtUtc = DateTimeOffset.UtcNow;
                msg.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Outbox Event [{Id}] ({EventType})", msg.Id, msg.EventType);
                msg.RetryCount++;
                msg.Error = ex.Message;
                if (msg.RetryCount >= 5)
                {
                    msg.Status = "Failed";
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchOutboxNotificationAsync(
        BooklineDbContext dbContext,
        OutboxMessage msg,
        IEmailSender? emailSender,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(msg.Content) ? "{}" : msg.Content);
        var root = doc.RootElement;

        string subject;
        string body;
        NotificationType notifType;
        string deepLink = "/dashboard";
        Guid? customerId = null;
        Guid? bookingId = null;
        string recipientEmail = "customer@bookline.local";
        string? recipientPhone = null;

        if (root.TryGetProperty("CustomerId", out var cIdProp) && cIdProp.TryGetGuid(out var parsedCId)) customerId = parsedCId;
        if (root.TryGetProperty("BookingId", out var bIdProp) && bIdProp.TryGetGuid(out var parsedBId)) bookingId = parsedBId;
        if (root.TryGetProperty("CustomerEmail", out var ceProp)) recipientEmail = ceProp.GetString() ?? recipientEmail;
        if (root.TryGetProperty("CustomerPhone", out var cpProp)) recipientPhone = cpProp.GetString();

        switch (msg.EventType)
        {
            case NotificationEvents.AppointmentCreated:
                notifType = NotificationType.BookingConfirmation;
                var refCode = root.TryGetProperty("BookingReference", out var br) ? br.GetString() : "BL-CONFIRM";
                subject = $"Appointment Created: {refCode}";
                body = $"Your appointment reservation ({refCode}) has been received and scheduled.";
                deepLink = "/appointments";
                break;

            case NotificationEvents.AppointmentConfirmed:
                notifType = NotificationType.BookingConfirmation;
                subject = "Appointment Confirmed";
                body = "Your booking has been officially confirmed by your service provider.";
                deepLink = "/appointments";
                break;

            case NotificationEvents.AppointmentCancelled:
                notifType = NotificationType.BookingCancellation;
                var cancelReason = root.TryGetProperty("Reason", out var r) ? r.GetString() : "Schedule change";
                subject = "Appointment Cancelled";
                body = $"Your appointment has been cancelled. Reason: {cancelReason}.";
                deepLink = "/appointments";
                break;

            case NotificationEvents.AppointmentRescheduled:
                notifType = NotificationType.BookingRescheduled;
                subject = "Appointment Rescheduled";
                body = "Your appointment has been rescheduled to a new confirmed time.";
                deepLink = "/appointments";
                break;

            case NotificationEvents.ReminderDue:
                var window = root.TryGetProperty("ReminderWindow", out var rw) ? rw.GetString() : "24h";
                notifType = window == "2h" ? NotificationType.Reminder2h : NotificationType.Reminder24h;
                subject = window == "2h" ? "Appointment in 2 Hours" : "Upcoming Appointment Tomorrow (24h)";
                var timeStr = root.TryGetProperty("LocalTimeString", out var lt) ? lt.GetString() : "scheduled time";
                body = $"Reminder: Your upcoming session is scheduled at {timeStr}.";
                deepLink = "/appointments";
                break;

            case NotificationEvents.OrderCreated:
                notifType = NotificationType.OrderUpdate;
                var orderNum = root.TryGetProperty("OrderNumber", out var on) ? on.GetString() : "ORD-NEW";
                var total = root.TryGetProperty("TotalAmount", out var ta) ? ta.GetDecimal() : 0m;
                var curr = root.TryGetProperty("Currency", out var cu) ? cu.GetString() : "₹";
                subject = $"Retail Commerce Order Placed: {orderNum}";
                body = $"Thank you! Order {orderNum} for {curr}{total:F2} is being fulfilled.";
                deepLink = "/orders";
                break;

            case NotificationEvents.PaymentSucceeded:
                notifType = NotificationType.PaymentReceipt;
                var txnRef = root.TryGetProperty("TransactionReference", out var tr) ? tr.GetString() : "TXN-OK";
                subject = "Payment Receipt Confirmed";
                body = $"Payment transaction {txnRef} was verified and processed successfully.";
                deepLink = "/dashboard";
                break;

            case NotificationEvents.PaymentFailed:
                notifType = NotificationType.SystemAlert;
                subject = "Payment Authorization Required";
                body = "A payment authorization attempt could not be verified. Please review payment method.";
                deepLink = "/dashboard";
                break;

            case NotificationEvents.RefundCreated:
                notifType = NotificationType.PaymentReceipt;
                var refId = root.TryGetProperty("RefundReference", out var rf) ? rf.GetString() : "REF-OK";
                subject = "Refund Processed";
                body = $"Refund {refId} has been issued back to your payment account.";
                deepLink = "/dashboard";
                break;

            case NotificationEvents.ReviewCreated:
                notifType = NotificationType.CustomMessage;
                var reviewer = root.TryGetProperty("CustomerName", out var cn) ? cn.GetString() : "A customer";
                var rating = root.TryGetProperty("Rating", out var rat) ? rat.GetInt32() : 5;
                subject = $"New {rating}-Star Review Received";
                body = $"{reviewer} published a {rating}-star verified review for your storefront.";
                deepLink = "/reviews";
                break;

            default:
                notifType = NotificationType.CustomMessage;
                subject = $"Platform Event: {msg.EventType}";
                body = "System notification event processed.";
                deepLink = "/dashboard";
                break;
        }

        var notificationLog = new NotificationLog
        {
            TenantId = msg.TenantId,
            BookingId = bookingId,
            CustomerId = customerId,
            RecipientEmail = recipientEmail,
            RecipientPhone = recipientPhone,
            NotificationType = notifType,
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Sent,
            Subject = subject,
            Body = body,
            DeepLinkUrl = deepLink,
            IsRead = false,
            SentAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.NotificationLogs.Add(notificationLog);

        if (emailSender != null && !string.IsNullOrWhiteSpace(recipientEmail))
        {
            try
            {
                await emailSender.SendEmailAsync(recipientEmail, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email delivery failed for notification log {Id}", notificationLog.Id);
            }
        }
    }

    private async Task ProcessRemindersAsync(BooklineDbContext dbContext, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        // 24-hour reminder window: bookings between [23.5h, 24.5h] ahead
        var window24hStart = nowUtc.AddHours(23.5);
        var window24hEnd = nowUtc.AddHours(24.5);

        // 2-hour reminder window: bookings between [1.75h, 2.25h] ahead
        var window2hStart = nowUtc.AddHours(1.75);
        var window2hEnd = nowUtc.AddHours(2.25);

        var upcomingBookings = await dbContext.Bookings.IgnoreQueryFilters()
            .Where(b => b.Status == BookingStatus.Confirmed
                     && ((b.StartUtc >= window24hStart && b.StartUtc <= window24hEnd) ||
                         (b.StartUtc >= window2hStart && b.StartUtc <= window2hEnd)))
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .ToListAsync(cancellationToken);

        if (!upcomingBookings.Any()) return;

        var tenants = await dbContext.Tenants.IgnoreQueryFilters()
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var booking in upcomingBookings)
        {
            tenants.TryGetValue(booking.TenantId, out var tenant);
            var tzId = tenant?.TimeZoneId ?? "Asia/Kolkata";
            var timeZone = ResolveTimeZone(tzId);

            var localTime = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc.UtcDateTime, timeZone);
            var is24hWindow = booking.StartUtc >= window24hStart && booking.StartUtc <= window24hEnd;
            var windowLabel = is24hWindow ? "24h" : "2h";
            var targetNotifType = is24hWindow ? NotificationType.Reminder24h : NotificationType.Reminder2h;

            // Idempotency check (Section 92): ensure reminder was not already scheduled or dispatched
            var alreadyProcessed = await dbContext.NotificationLogs.IgnoreQueryFilters()
                .AnyAsync(n => n.BookingId == booking.Id && n.NotificationType == targetNotifType, cancellationToken);

            if (alreadyProcessed) continue;

            var alreadyInOutbox = await dbContext.OutboxMessages.IgnoreQueryFilters()
                .AnyAsync(o => o.EventType == NotificationEvents.ReminderDue
                            && o.Content.Contains(booking.Id.ToString())
                            && o.Content.Contains(windowLabel), cancellationToken);

            if (alreadyInOutbox) continue;

            // Atomic business change + audit + outbox event commit (Section 91 & 92)
            dbContext.AuditLogs.Add(new AuditLog
            {
                TenantId = booking.TenantId,
                Actor = "SystemReminderWorker",
                Action = $"Reminder.{windowLabel}.Scheduled",
                Target = booking.Id.ToString(),
                MetadataJson = JsonSerializer.Serialize(new
                {
                    BookingId = booking.Id,
                    Window = windowLabel,
                    LocalTime = localTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
            });

            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                TenantId = booking.TenantId,
                EventType = NotificationEvents.ReminderDue,
                Content = JsonSerializer.Serialize(new
                {
                    BookingId = booking.Id,
                    BookingReference = booking.BookingReference,
                    TenantId = booking.TenantId,
                    CustomerId = booking.CustomerId,
                    CustomerEmail = booking.Customer?.Email ?? "customer@bookline.local",
                    CustomerPhone = booking.Customer?.Phone,
                    ServiceName = booking.Service?.Name ?? "Service",
                    ReminderWindow = windowLabel,
                    StartUtc = booking.StartUtc,
                    LocalTimeString = $"{localTime:h:mm tt} on {localTime:dddd, MMMM d}"
                })
            });

            booking.MarkReminderSent(DateTimeOffset.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            try
            {
                // Fallback for Windows ID mappings
                if (timeZoneId.Contains("Kolkata") || timeZoneId.Contains("Calcutta"))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                }
                return TimeZoneInfo.Utc;
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
