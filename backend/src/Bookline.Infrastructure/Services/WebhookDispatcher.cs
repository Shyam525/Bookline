namespace Bookline.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IApplicationDbContext _context;
    private readonly HttpClient _httpClient;

    public WebhookDispatcher(IApplicationDbContext context, HttpClient? httpClient = null)
    {
        _context = context;
        _httpClient = httpClient ?? new HttpClient();
    }

    public string ComputeSignature(string payloadJson, string secret, long timestamp)
    {
        var toSign = $"{timestamp}.{payloadJson}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
        var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return $"t={timestamp},v1={hashHex}";
    }

    public async Task DispatchEventAsync<T>(Guid tenantId, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.Webhooks
            .IgnoreQueryFilters()
            .Where(w => w.TenantId == tenantId && w.IsActive && (w.EventTypes == "*" || w.EventTypes.Contains(eventType)))
            .ToListAsync(cancellationToken);

        if (!subscriptions.Any()) return;

        var json = JsonSerializer.Serialize(payload);

        foreach (var sub in subscriptions)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = ComputeSignature(json, sub.Secret, timestamp);

            var request = new HttpRequestMessage(HttpMethod.Post, sub.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Bookline-Signature", signature);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var log = new WebhookDeliveryLog
                {
                    TenantId = tenantId,
                    SubscriptionId = sub.Id,
                    EventType = eventType,
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    AttemptedAtUtc = DateTimeOffset.UtcNow
                };
                _context.WebhookLogs.Add(log);
            }
            catch (Exception ex)
            {
                var log = new WebhookDeliveryLog
                {
                    TenantId = tenantId,
                    SubscriptionId = sub.Id,
                    EventType = eventType,
                    StatusCode = 500,
                    IsSuccess = false,
                    ResponsePayload = ex.Message,
                    AttemptedAtUtc = DateTimeOffset.UtcNow
                };
                _context.WebhookLogs.Add(log);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
