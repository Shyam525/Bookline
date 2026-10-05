# Bookline Multi-Tenancy Architecture

## Tenant Isolation Strategy

Bookline enforces strict database-level multi-tenant data isolation.

### Isolation Rules
1. **Tenant Entity Base**: All tenant-scoped entities inherit from `TenantEntity` (`public Guid TenantId { get; set; }`).
2. **EF Core Global Query Filters**: Applied automatically to all EF queries:
   ```csharp
   builder.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
   ```
3. **Save Interceptor**: `TenantSaveChangesInterceptor` automatically populates `TenantId` on newly added `TenantEntity` objects during `SaveChangesAsync()`.
4. **Tenant Context**: Scope-bound `ITenantContext` injected into API controllers and handlers.

---
*Updated for Phase 4 - Phase 10 compliance.*
