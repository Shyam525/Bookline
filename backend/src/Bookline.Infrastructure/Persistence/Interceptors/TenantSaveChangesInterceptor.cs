using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Common;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookline.Infrastructure.Persistence.Interceptors;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantSaveChangesInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTenantEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateTenantEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateTenantEntities(DbContext? context)
    {
        if (context == null) return;
        if (_tenantContext.IsSystem) return;

        foreach (var entry in context.ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.Entity is AppUser user && (user.Role == "PlatformAdmin" || user.Role == "Customer" || user.TenantId == Guid.Empty))
            {
                continue;
            }

            if (entry.Entity is RefreshToken rt && rt.TenantId == Guid.Empty)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                if (!_tenantContext.IsResolved)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        throw new InvalidOperationException("Cannot save tenant entity without an active tenant context.");
                    }
                }
                else
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = _tenantContext.TenantId;
                    }
                    else if (entry.Entity.TenantId != _tenantContext.TenantId)
                    {
                        throw new InvalidOperationException("Entity TenantId does not match current TenantContext.");
                    }
                }
            }
            else if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                if (!_tenantContext.IsResolved || entry.Entity.TenantId != _tenantContext.TenantId)
                {
                    throw new InvalidOperationException("Cross-tenant modification or deletion attempted.");
                }
            }
        }
    }
}
