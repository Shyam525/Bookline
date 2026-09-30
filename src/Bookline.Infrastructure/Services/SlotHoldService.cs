namespace Bookline.Infrastructure.Services;

using System.Collections.Concurrent;
using Bookline.Application.Common.Interfaces;
using StackExchange.Redis;

public class SlotHoldService : ISlotHoldService
{
    private record HoldEntry(string HoldId, DateTimeOffset ExpiresAt);

    private readonly IConnectionMultiplexer? _redis;
    private static readonly ConcurrentDictionary<string, HoldEntry> _inMemoryHolds = new();

    public SlotHoldService(IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
    }

    public async Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startUtc);
        var holdId = Guid.NewGuid();

        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var acquired = await db.StringSetAsync(key, holdId.ToString(), TimeSpan.FromSeconds(300), When.NotExists);
            return acquired ? holdId : null;
        }

        // In-memory fallback for unit testing / redis-less execution
        CleanupExpiredHolds();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(300);

        var entry = new HoldEntry(holdId.ToString(), expiresAt);
        var added = _inMemoryHolds.TryAdd(key, entry);
        if (added)
        {
            return holdId;
        }

        if (_inMemoryHolds.TryGetValue(key, out var existing))
        {
            if (existing.ExpiresAt <= now)
            {
                _inMemoryHolds.TryRemove(key, out _);
                if (_inMemoryHolds.TryAdd(key, entry))
                {
                    return holdId;
                }
            }
        }

        return null;
    }

    public async Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startUtc);

        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue && value.ToString() == holdId.ToString();
        }

        CleanupExpiredHolds();
        if (_inMemoryHolds.TryGetValue(key, out var existing))
        {
            return existing.ExpiresAt > DateTimeOffset.UtcNow && existing.HoldId == holdId.ToString();
        }

        return false;
    }

    public async Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default)
    {
        var key = BuildHoldKey(tenantId, staffId, startUtc);

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

    private static string BuildHoldKey(Guid tenantId, Guid staffId, DateTimeOffset startUtc)
    {
        return $"hold:{tenantId}:{staffId}:{startUtc.ToUnixTimeSeconds()}";
    }

    private static void CleanupExpiredHolds()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in _inMemoryHolds)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                _inMemoryHolds.TryRemove(pair.Key, out _);
            }
        }
    }
}
