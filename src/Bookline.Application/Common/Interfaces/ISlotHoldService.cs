namespace Bookline.Application.Common.Interfaces;

public interface ISlotHoldService
{
    Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, TimeSpan duration, CancellationToken cancellationToken = default);
    Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default);
    Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default);
}
