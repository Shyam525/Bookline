namespace Bookline.Application.Notifications.Handlers;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Notifications.DTOs;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public record GetNotificationLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? StatusFilter = null,
    string? ChannelFilter = null
);

public record GetNotificationSettingsQuery();

public record UpdateNotificationSettingsCommand(
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    bool Reminder24hEnabled,
    bool Reminder1hEnabled,
    string SenderEmail,
    string SenderName
);

public record SendTestNotificationCommand(
    string RecipientEmail,
    string? RecipientPhone,
    string Channel
);

public class NotificationHandlers
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly INotificationService _notificationService;

    public NotificationHandlers(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        INotificationService notificationService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<NotificationLogDto>> Handle(GetNotificationLogsQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var dbQuery = _context.NotificationLogs
            .Where(n => n.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.StatusFilter) && Enum.TryParse<NotificationStatus>(query.StatusFilter, true, out var status))
        {
            dbQuery = dbQuery.Where(n => n.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.ChannelFilter) && Enum.TryParse<NotificationChannel>(query.ChannelFilter, true, out var channel))
        {
            dbQuery = dbQuery.Where(n => n.Channel == channel);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationLogDto(
                n.Id,
                n.TenantId,
                n.BookingId,
                n.CustomerId,
                n.RecipientEmail,
                n.RecipientPhone,
                n.NotificationType.ToString(),
                n.Channel.ToString(),
                n.Status.ToString(),
                n.Subject,
                n.Body,
                n.ErrorMessage,
                n.SentAtUtc,
                n.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationLogDto>(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<NotificationSettingDto> Handle(GetNotificationSettingsQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (setting == null)
        {
            setting = new NotificationSetting
            {
                TenantId = tenantId,
                EmailNotificationsEnabled = true,
                SmsNotificationsEnabled = false,
                Reminder24hEnabled = true,
                Reminder1hEnabled = true,
                SenderEmail = "no-reply@bookline.io",
                SenderName = "Bookline Appointments"
            };

            _context.NotificationSettings.Add(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapSetting(setting);
    }

    public async Task<NotificationSettingDto> Handle(UpdateNotificationSettingsCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        if (string.IsNullOrWhiteSpace(command.SenderEmail))
        {
            throw new ValidationException("Sender email is required.");
        }

        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (setting == null)
        {
            setting = new NotificationSetting { TenantId = tenantId };
            _context.NotificationSettings.Add(setting);
        }

        setting.EmailNotificationsEnabled = command.EmailNotificationsEnabled;
        setting.SmsNotificationsEnabled = command.SmsNotificationsEnabled;
        setting.Reminder24hEnabled = command.Reminder24hEnabled;
        setting.Reminder1hEnabled = command.Reminder1hEnabled;
        setting.SenderEmail = command.SenderEmail;
        setting.SenderName = command.SenderName;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapSetting(setting);
    }

    public async Task<bool> Handle(SendTestNotificationCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        if (string.IsNullOrWhiteSpace(command.RecipientEmail))
        {
            throw new ValidationException("Recipient email is required for test notification.");
        }

        var channel = Enum.TryParse<NotificationChannel>(command.Channel, true, out var parsedChannel)
            ? parsedChannel
            : NotificationChannel.Email;

        await _notificationService.SendNotificationAsync(
            tenantId: tenantId,
            type: NotificationType.CustomMessage,
            channel: channel,
            recipientEmail: command.RecipientEmail,
            recipientPhone: command.RecipientPhone,
            subject: "Bookline Test Notification",
            body: "This is a test notification sent from your Bookline workspace to verify your email/SMS notification settings.",
            cancellationToken: cancellationToken
        );

        return true;
    }

    private static NotificationSettingDto MapSetting(NotificationSetting s) => new(
        s.Id,
        s.TenantId,
        s.EmailNotificationsEnabled,
        s.SmsNotificationsEnabled,
        s.Reminder24hEnabled,
        s.Reminder1hEnabled,
        s.SenderEmail,
        s.SenderName,
        s.CreatedAtUtc,
        s.UpdatedAtUtc
    );
}
