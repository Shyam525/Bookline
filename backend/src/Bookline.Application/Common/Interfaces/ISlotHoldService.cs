using NodaTime;

namespace Bookline.Application.Common.Interfaces;

public interface ISlotHoldService
{
    Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, TimeSpan duration, CancellationToken cancellationToken = default);
    Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default);
    Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default);

    Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Duration duration, CancellationToken cancellationToken = default)
        => AcquireHoldAsync(tenantId, staffId, startInstant.ToDateTimeOffset(), duration.ToTimeSpan(), cancellationToken);

    Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Guid holdId, CancellationToken cancellationToken = default)
        => ValidateHoldAsync(tenantId, staffId, startInstant.ToDateTimeOffset(), holdId, cancellationToken);

    Task ReleaseHoldAsync(Guid tenantId, Guid staffId, Instant startInstant, Guid holdId, CancellationToken cancellationToken = default)
        => ReleaseHoldAsync(tenantId, staffId, startInstant.ToDateTimeOffset(), holdId, cancellationToken);
}
