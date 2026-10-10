using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Services;

/// <summary>
/// Database-backed idempotency service ensuring Section 121 repeatable operation safety.
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly BooklineDbContext _dbContext;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(BooklineDbContext dbContext, ILogger<IdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IdempotencyResult?> GetExistingAsync(string key, string operation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var record = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.Operation == operation, cancellationToken);

        if (record == null) return null;

        // Verify record is not expired
        if (record.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Idempotency key {Key} for {Operation} has expired.", key, operation);
            return null;
        }

        _logger.LogInformation("Idempotency HIT for key {Key} ({Operation}). Returning cached status {Status}.",
            key, operation, record.StatusCode);

        return new IdempotencyResult(record.StatusCode, record.ResponseJson, record.CreatedAtUtc);
    }

    public async Task SaveAsync(
        string key,
        string operation,
        int statusCode,
        string responseJson,
        Guid? tenantId = null,
        string? requestHash = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        try
        {
            var existing = await _dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == key && r.Operation == operation, cancellationToken);

            if (existing != null)
            {
                existing.StatusCode = statusCode;
                existing.ResponseJson = responseJson;
                existing.ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(24);
            }
            else
            {
                _dbContext.IdempotencyRecords.Add(new IdempotencyRecord
                {
                    Key = key,
                    Operation = operation,
                    TenantId = tenantId,
                    RequestHash = requestHash,
                    StatusCode = statusCode,
                    ResponseJson = responseJson,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(24)
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved idempotency record for key {Key} ({Operation}).", key, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist idempotency record for key {Key}.", key);
        }
    }
}
