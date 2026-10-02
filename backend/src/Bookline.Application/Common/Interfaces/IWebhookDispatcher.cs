namespace Bookline.Application.Common.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchEventAsync<T>(Guid tenantId, string eventType, T payload, CancellationToken cancellationToken = default);
    string ComputeSignature(string payloadJson, string secret, long timestamp);
}
