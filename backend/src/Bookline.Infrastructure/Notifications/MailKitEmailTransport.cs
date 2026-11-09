namespace Bookline.Infrastructure.Notifications;

using System;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Enums;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

public class MailKitEmailTransport : INotificationTransport
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly ILogger<MailKitEmailTransport> _logger;

    public string TransportName => "MailKitSmtpTransport";

    public MailKitEmailTransport(IConfiguration configuration, ILogger<MailKitEmailTransport> logger)
    {
        _logger = logger;
        _smtpHost = configuration["Email:SmtpHost"] 
            ?? configuration["Smtp:Host"] 
            ?? Environment.GetEnvironmentVariable("SMTP_HOST") 
            ?? "localhost";

        var portStr = configuration["Email:SmtpPort"] 
            ?? configuration["Smtp:Port"] 
            ?? Environment.GetEnvironmentVariable("SMTP_PORT") 
            ?? "1026";

        _smtpPort = int.TryParse(portStr, out var port) ? port : 1026;
        _senderEmail = configuration["Email:SenderEmail"] ?? "noreply@bookline.local";
        _senderName = configuration["Email:SenderName"] ?? "Bookline Appointments";
    }

    public bool CanHandle(NotificationChannel channel) => channel == NotificationChannel.Email;

    public async Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            return NotificationDeliveryResult.Failed("Recipient email cannot be empty.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(request.RecipientName ?? request.Recipient, request.Recipient));
            message.Subject = request.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"<div style=\"font-family: sans-serif; font-size: 14px; line-height: 1.6;\"><p>{request.Body}</p></div>",
                TextBody = request.Body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.None, cancellationToken);
            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Delivered email notification to {Recipient} via {Host}:{Port}", request.Recipient, _smtpHost, _smtpPort);
            return NotificationDeliveryResult.Succeeded($"smtp-{Guid.NewGuid():N}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver email notification to {Recipient} via {Host}:{Port}: {Error}", request.Recipient, _smtpHost, _smtpPort, ex.Message);
            return NotificationDeliveryResult.Failed(ex.Message);
        }
    }
}

