using System.Collections.Generic;

namespace Bookline.Application.Common.Models;

/// <summary>
/// Universal stable API error response envelope adhering to Section 82:
/// {
///   "code": "SLOT_UNAVAILABLE",
///   "message": "This time is no longer available.",
///   "traceId": "server-generated-correlation-id"
/// }
/// </summary>
public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public int Status { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
    public IDictionary<string, object?>? Extensions { get; set; }

    public ApiErrorResponse() { }

    public ApiErrorResponse(string code, string message, string traceId, int status = 400)
    {
        Code = code;
        Message = message;
        TraceId = traceId;
        Status = status;
    }

    public static ApiErrorResponse Validation(string traceId, IDictionary<string, string[]> errors, string message = "One or more validation errors occurred.") =>
        new("VALIDATION_FAILED", message, traceId, 400) { Errors = errors };

    public static ApiErrorResponse NotFound(string message, string traceId) =>
        new("NOT_FOUND", message, traceId, 404);

    public static ApiErrorResponse Conflict(string code, string message, string traceId, IDictionary<string, object?>? extensions = null) =>
        new(code, message, traceId, 409) { Extensions = extensions };

    public static ApiErrorResponse Unauthenticated(string traceId, string message = "Authentication is required to access this resource.") =>
        new("UNAUTHENTICATED", message, traceId, 401);

    public static ApiErrorResponse Forbidden(string traceId, string message = "You do not have permission to access this resource.") =>
        new("FORBIDDEN", message, traceId, 403);

    public static ApiErrorResponse RateLimited(string traceId, string message = "Too many requests. Please wait a moment before trying again.") =>
        new("RATE_LIMITED", message, traceId, 429);

    public static ApiErrorResponse InternalError(string traceId, string message = "An unexpected application error occurred.") =>
        new("INTERNAL_SERVER_ERROR", message, traceId, 500);

    public static ApiErrorResponse ServiceUnavailable(string traceId, string message = "The service is temporarily unavailable. Please retry in a few moments.") =>
        new("SERVICE_UNAVAILABLE", message, traceId, 503);
}
