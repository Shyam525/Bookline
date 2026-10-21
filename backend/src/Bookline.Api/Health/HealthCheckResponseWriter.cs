using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bookline.Api.Health;

/// <summary>
/// Section 118: Sanitized, standard JSON health check response writer.
/// Guarantees that no connection strings, credentials, SQL queries, or internal stack traces
/// are ever leaked to public callers.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? "Operational",
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2)
            })
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
