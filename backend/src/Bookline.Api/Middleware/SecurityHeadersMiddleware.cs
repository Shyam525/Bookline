namespace Bookline.Api.Middleware;

/// <summary>
/// Security headers middleware adhering to Section 116 security standards.
/// Injects defense-in-depth headers:
/// - X-Content-Type-Options: nosniff
/// - X-Frame-Options: DENY (anti-clickjacking)
/// - X-XSS-Protection: 1; mode=block
/// - Referrer-Policy: strict-origin-when-cross-origin
/// - Permissions-Policy
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(self), camera=(), microphone=()");

        await _next(context);
    }
}
