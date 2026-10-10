namespace Bookline.Application.Common.Interfaces;

public record IdempotencyResult(int StatusCode, string ResponseJson, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Service interface guaranteeing Section 121 idempotency contract.
/// Ensures critical operations (booking, payment, refund, order, webhook, notification)
/// are safely repeatable.
/// </summary>
public interface IIdempotencyService
{
    Task<IdempotencyResult?> GetExistingAsync(string key, string operation, CancellationToken cancellationToken = default);
    Task SaveAsync(string key, string operation, int statusCode, string responseJson, Guid? tenantId = null, string? requestHash = null, CancellationToken cancellationToken = default);
}
