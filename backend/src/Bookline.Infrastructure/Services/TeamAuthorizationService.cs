using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Infrastructure.Services;

public class TeamAuthorizationService : ITeamAuthorizationService
{
    private readonly BooklineDbContext _dbContext;

    public TeamAuthorizationService(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProviderTeamRole?> GetUserRoleInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // 1. Check PlatformAdmin bypass
        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null && user.Role == "PlatformAdmin")
        {
            return ProviderTeamRole.Owner;
        }

        // 2. Check OrganizationMembership
        var membership = await _dbContext.OrganizationMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, cancellationToken);

        if (membership != null && Enum.TryParse<ProviderTeamRole>(membership.Role, true, out var role))
        {
            return role;
        }

        // 3. Check if primary user is tenant owner
        if (user != null && user.TenantId == tenantId && (user.Role == "Owner" || user.Role == "Provider"))
        {
            return ProviderTeamRole.Owner;
        }

        return null;
    }

    public async Task<bool> HasMinimumRoleAsync(Guid userId, Guid tenantId, ProviderTeamRole minimumRole, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        if (role == null) return false;

        // In ProviderTeamRole: Owner(0) < Admin(1) < Manager(2) < Receptionist(3) < Staff(4) < Viewer(5)
        return (int)role <= (int)minimumRole;
    }

    public async Task<bool> CanManageTeamAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role is ProviderTeamRole.Owner or ProviderTeamRole.Admin;
    }

    public async Task<bool> CanManageServicesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role is ProviderTeamRole.Owner or ProviderTeamRole.Admin or ProviderTeamRole.Manager;
    }

    public async Task<bool> CanManageStaffAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role is ProviderTeamRole.Owner or ProviderTeamRole.Admin or ProviderTeamRole.Manager;
    }

    public async Task<bool> CanManageBookingsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role is ProviderTeamRole.Owner or ProviderTeamRole.Admin or ProviderTeamRole.Manager or ProviderTeamRole.Receptionist;
    }

    public async Task<bool> CanManageFinancialsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role is ProviderTeamRole.Owner;
    }

    public async Task<bool> CanMutateDataAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleInTenantAsync(userId, tenantId, cancellationToken);
        return role != null && role != ProviderTeamRole.Viewer;
    }
}
