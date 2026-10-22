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
        var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? context.User.FindFirst("role")?.Value;

        var isPlatformAdmin = string.Equals(roleClaim, "PlatformAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimTenantId))
        {
            if (claimTenantId != Guid.Empty)
            {
                // Platform admin can override tenant context via header for diagnostics/support
                if (isPlatformAdmin &&
                    context.Request.Headers.TryGetValue("X-Tenant-Id", out var adminOverrideHeader) &&
                    Guid.TryParse(adminOverrideHeader.ToString(), out var overrideId) &&
                    overrideId != Guid.Empty)
                {
                    tenantContext.SetTenantId(overrideId);
                }
                else
                {
                    tenantContext.SetTenantId(claimTenantId);
                }
            }
            else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId) &&
                     Guid.TryParse(headerTenantId.ToString(), out var parsedHeaderTenantId) &&
                     parsedHeaderTenantId != Guid.Empty)
            {
                // Authenticated customer or platform admin:
                // Only allow setting tenant context on read requests, or for PlatformAdmin,
                // preventing customer accounts from escalating to provider mutation privileges.
                var isSafeMethod = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);
                var isPublicPath = context.Request.Path.StartsWithSegments("/api/public") || context.Request.Path.StartsWithSegments("/api/v1/public");

                if (isPlatformAdmin || isSafeMethod || isPublicPath)
                {
                    tenantContext.SetTenantId(parsedHeaderTenantId);
                }
            }
        }
        // 2. Check X-Tenant-Id header for unauthenticated public requests
        else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId) &&
                 Guid.TryParse(headerTenantId.ToString(), out var parsedHeaderTenantId) &&
                 parsedHeaderTenantId != Guid.Empty)
        {
            tenantContext.SetTenantId(parsedHeaderTenantId);
        }

        await _next(context);
    }
}
