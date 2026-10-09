using Bookline.Domain.Enums;

namespace Bookline.Application.Common.Interfaces;

public interface ITeamAuthorizationService
{
    Task<ProviderTeamRole?> GetUserRoleInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasMinimumRoleAsync(Guid userId, Guid tenantId, ProviderTeamRole minimumRole, CancellationToken cancellationToken = default);
    Task<bool> CanManageTeamAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanManageServicesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanManageStaffAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanManageBookingsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanManageFinancialsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanMutateDataAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
