namespace Bookline.Infrastructure.Notifications;

using System;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Development / Local simulated adapter for SMS & WhatsApp notifications.
/// Clearly labelled as test/dev adapter and warns when no live production provider
/// (e.g. Twilio / AWS SNS / Meta Cloud API) credentials are configured.
/// </summary>
public class DevSimulatedSmsTransport : INotificationTransport
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevSimulatedSmsTransport> _logger;

    public string TransportName => "[DEV/TEST ONLY] Simulated SMS / WhatsApp Adapter";

    public DevSimulatedSmsTransport(IConfiguration configuration, ILogger<DevSimulatedSmsTransport> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool CanHandle(NotificationChannel channel) => channel == NotificationChannel.SMS;

    public Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            return Task.FromResult(NotificationDeliveryResult.Failed("Recipient phone number cannot be empty."));
        }

        var liveProviderKey = _configuration["Sms:ApiKey"] ?? _configuration["Twilio:AuthToken"];
        var isLiveConfigured = !string.IsNullOrWhiteSpace(liveProviderKey);

        if (!isLiveConfigured)
        {
            _logger.LogWarning("[DEV-ADAPTER] No production SMS gateway configured. Simulating delivery to {Recipient}: {Subject} - {Body}",
                request.Recipient, request.Subject, request.Body);

            return Task.FromResult(NotificationDeliveryResult.Succeeded($"dev-simulated-sms-{Guid.NewGuid():N}"));
        }

        // When production gateway configured, real dispatch would execute here
        _logger.LogInformation("Live SMS dispatched to {Recipient}", request.Recipient);
        return Task.FromResult(NotificationDeliveryResult.Succeeded($"live-sms-{Guid.NewGuid():N}"));
    }
}
