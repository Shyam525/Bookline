namespace Bookline.Infrastructure.Notifications;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

public class NotificationTransportDispatcher : INotificationTransportDispatcher
{
    private readonly IEnumerable<INotificationTransport> _transports;
    private readonly ILogger<NotificationTransportDispatcher> _logger;

    public NotificationTransportDispatcher(IEnumerable<INotificationTransport> transports, ILogger<NotificationTransportDispatcher> logger)
    {
        _transports = transports;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> DispatchAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var transport = _transports.FirstOrDefault(t => t.CanHandle(request.Channel));
        if (transport == null)
        {
            var msg = $"No notification transport registered to handle channel {request.Channel}.";
            _logger.LogWarning(msg);
            return NotificationDeliveryResult.Failed(msg);
        }

        _logger.LogInformation("Dispatching {Channel} notification for Tenant {TenantId} via {TransportName}",
            request.Channel, request.TenantId, transport.TransportName);

        try
        {
            return await transport.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in transport {TransportName} during dispatch to {Recipient}",
                transport.TransportName, request.Recipient);
            return NotificationDeliveryResult.Failed(ex.Message);
        }
    }
}

