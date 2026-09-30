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
            tenantContext.SetTenantId(claimTenantId);
        }
        // 2. Check X-Tenant-Id header for tenant-scoped unauthenticated endpoints
        else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId) &&
                 Guid.TryParse(headerTenantId.ToString(), out var parsedHeaderTenantId))
        {
            tenantContext.SetTenantId(parsedHeaderTenantId);
        }

        await _next(context);
    }
}
