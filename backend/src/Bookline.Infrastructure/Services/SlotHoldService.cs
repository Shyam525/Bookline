namespace Bookline.Infrastructure.Services;

using System.Collections.Concurrent;
using Bookline.Application.Common.Interfaces;
using NodaTime;
using StackExchange.Redis;

public class SlotHoldService : ISlotHoldService
{
    private record HoldEntry(string HoldId, Instant ExpiresAt);

    private readonly IConnectionMultiplexer? _redis;
    private readonly IClock _clock;
    private readonly ConcurrentDictionary<string, HoldEntry> _inMemoryHolds = new();

    public SlotHoldService(IConnectionMultiplexer? redis = null, IClock? clock = null)
    {
        _redis = redis;
        _clock = clock ?? SystemClock.Instance;
    }

    public Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        return AcquireHoldAsync(tenantId, staffId, Instant.FromDateTimeOffset(startUtc), Duration.FromTimeSpan(duration), cancellationToken);
    }

    public async Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Duration duration, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startInstant);
        var holdId = Guid.NewGuid();
        var holdDuration = duration > Duration.Zero ? duration : Duration.FromMinutes(5);

        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var acquired = await db.StringSetAsync(key, holdId.ToString(), holdDuration.ToTimeSpan(), When.NotExists);
            return acquired ? holdId : null;
        }

        // In-memory fallback for unit testing / redis-less execution
        lock (_inMemoryHolds)
        {
            CleanupExpiredHolds();
            var now = _clock.GetCurrentInstant();
            if (_inMemoryHolds.TryGetValue(key, out var existing) && existing.ExpiresAt > now)
            {
                return null;
            }

            var expiresAt = now + holdDuration;
            var entry = new HoldEntry(holdId.ToString(), expiresAt);
            _inMemoryHolds[key] = entry;
            return holdId;
        }
    }

    public Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default)
    {
        return ValidateHoldAsync(tenantId, staffId, Instant.FromDateTimeOffset(startUtc), holdId, cancellationToken);
    }

    public async Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Guid holdId, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startInstant);

        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue && value.ToString() == holdId.ToString();
        }

        CleanupExpiredHolds();
        if (_inMemoryHolds.TryGetValue(key, out var existing))
        {
            return existing.ExpiresAt > _clock.GetCurrentInstant() && existing.HoldId == holdId.ToString();
        }

        return false;
    }

    public Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default)
    {
        return ReleaseHoldAsync(tenantId, staffId, Instant.FromDateTimeOffset(startUtc), holdId, cancellationToken);
    }

    public async Task ReleaseHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Guid holdId, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startInstant);

        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.HasValue && value.ToString() == holdId.ToString())
            {
                await db.KeyDeleteAsync(key);
            }
            return;
        }

        if (_inMemoryHolds.TryGetValue(key, out var existing) && existing.HoldId == holdId.ToString())
        {
            _inMemoryHolds.TryRemove(key, out _);
        }
    }

    private static string BuildHoldKey(Guid tenantId, Guid staffId, Instant startInstant)
    {
        return $"hold:{tenantId}:{staffId}:{startInstant.ToUnixTimeSeconds()}";
    }

    private void CleanupExpiredHolds()
    {
        var now = _clock.GetCurrentInstant();
        foreach (var pair in _inMemoryHolds)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                _inMemoryHolds.TryRemove(pair.Key, out _);
            }
        }
    }
}
