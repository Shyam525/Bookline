namespace Bookline.Application.UnitTests;

using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Notifications.DTOs;
using Bookline.Application.Notifications.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class NotificationCommandHandlerTests
{
    private (BooklineDbContext DbContext, TestTenantContext TenantContext, INotificationService NotificationService) CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        var emailSender = new MailKitEmailSender(null!);
        var logger = NullLogger<NotificationService>.Instance;
        var notificationService = new NotificationService(context, emailSender, logger);

        return (context, tenantContext, notificationService);
    }

    [Fact]
    public async Task GetNotificationSettings_ShouldReturnDefaultSetting_WhenNotExists()
    {
        var tenantId = Guid.NewGuid();
        var (db, context, notificationService) = CreateDbContext(tenantId);

        var handlers = new NotificationHandlers(db, context, notificationService);

        var setting = await handlers.Handle(new GetNotificationSettingsQuery());

        Assert.NotNull(setting);
        Assert.True(setting.EmailNotificationsEnabled);
        Assert.False(setting.SmsNotificationsEnabled);
        Assert.Equal("no-reply@bookline.io", setting.SenderEmail);
    }

    [Fact]
    public async Task UpdateNotificationSettings_ShouldSaveNewPreferences()
    {
        var tenantId = Guid.NewGuid();
        var (db, context, notificationService) = CreateDbContext(tenantId);

        var handlers = new NotificationHandlers(db, context, notificationService);

        var command = new UpdateNotificationSettingsCommand(
            EmailNotificationsEnabled: true,
            SmsNotificationsEnabled: true,
            Reminder24hEnabled: true,
            Reminder1hEnabled: false,
            SenderEmail: "custom@barbershop.com",
            SenderName: "Lux Barbershop"
        );

        var updated = await handlers.Handle(command);

        Assert.NotNull(updated);
        Assert.True(updated.SmsNotificationsEnabled);
        Assert.False(updated.Reminder1hEnabled);
        Assert.Equal("custom@barbershop.com", updated.SenderEmail);
        Assert.Equal("Lux Barbershop", updated.SenderName);
    }

    [Fact]
    public async Task SendTestNotification_ShouldCreateNotificationLog()
    {
        var tenantId = Guid.NewGuid();
        var (db, context, notificationService) = CreateDbContext(tenantId);

        var handlers = new NotificationHandlers(db, context, notificationService);

        var result = await handlers.Handle(new SendTestNotificationCommand(
            RecipientEmail: "customer@example.com",
            RecipientPhone: "+15550199",
            Channel: "Email"
        ));

        Assert.True(result);

        var logs = await handlers.Handle(new GetNotificationLogsQuery());
        Assert.Single(logs.Items);
        Assert.Equal("customer@example.com", logs.Items[0].RecipientEmail);
        Assert.Equal("Sent", logs.Items[0].Status);
    }
}
