using Bookline.Application.Common.Interfaces;

namespace Bookline.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // 1. Check JWT tenant_id claim
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimTenantId))
        {
            if (claimTenantId != Guid.Empty)
            {
                tenantContext.SetTenantId(claimTenantId);
            }
            else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId) &&
                     Guid.TryParse(headerTenantId.ToString(), out var parsedHeaderTenantId))
            {
                // Authenticated customer accessing a specific tenant's public catalogue/storefront
                tenantContext.SetTenantId(parsedHeaderTenantId);
            }
        }
        // 2. Check X-Tenant-Id header for unauthenticated requests
        else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId) &&
                 Guid.TryParse(headerTenantId.ToString(), out var parsedHeaderTenantId))
        {
            tenantContext.SetTenantId(parsedHeaderTenantId);
        }

        await _next(context);
    }
}
