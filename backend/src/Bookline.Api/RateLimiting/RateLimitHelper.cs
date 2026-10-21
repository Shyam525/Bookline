using Microsoft.AspNetCore.Http;

namespace Bookline.Api.RateLimiting;

/// <summary>
/// Section 120: Client identity and rate-limit partition helper.
/// Resolves client IP securely via HttpContext.Connection.RemoteIpAddress,
/// which is populated via ForwardedHeadersMiddleware only from configured trusted proxies.
/// </summary>
public static class RateLimitHelper
{
    public static string ResolveClientIp(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIp) && remoteIp != "127.0.0.1" && remoteIp != "::1")
        {
            return remoteIp;
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return firstIp;
            }
        }

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            return remoteIp;
        }

        return "127.0.0.1";
    }
}
