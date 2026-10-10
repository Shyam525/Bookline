using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Bookline.Api.Middleware;

/// <summary>
/// Observability middleware adhering to Section 124.
/// Enriches every incoming request with a correlation ID, sets structured logging context,
/// and returns the X-Correlation-ID header on outgoing HTTP responses.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate _next)
    {
        this._next = _next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push correlation ID and trace ID into structured Serilog logging context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}
