# Bookline Multi-Tenancy & Data Isolation

## Tenant Context Resolution

Multi-tenancy is enforced server-side. The backend resolves the active tenant context using `TenantContextMiddleware` and `ITenantContext`:

1. **Authenticated Users**: Resolved via JWT claim (`tenant_id`).
2. **Public Booking Pages**: Resolved via route/query slug (e.g. `/api/v1/public/tenant/acme-salon`).

## EF Core Global Query Filters

All tenant-owned entities inherit from `TenantEntity`:

```csharp
public abstract class TenantEntity
{
    public Guid TenantId { get; set; }
}
```

`BooklineDbContext` automatically configures a global query filter on all `TenantEntity` types:

```csharp
modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
```

## Cross-Tenant Security

Public queries explicitly invoke `.IgnoreQueryFilters()` only to cross-check requested resource tenant IDs against the resolved tenant slug, throwing `NotFoundException` (HTTP 404) on any mismatch.
