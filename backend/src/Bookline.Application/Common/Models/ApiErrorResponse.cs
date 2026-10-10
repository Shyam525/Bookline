using System.Collections.Generic;

namespace Bookline.Application.Common.Models;

/// <summary>
/// Universal stable API error response envelope adhering to Section 113.
/// Standard structure:
/// {
///   "code": "SLOT_UNAVAILABLE",
///   "message": "That time was just booked. Please choose another time.",
///   "traceId": "00-53bc77..."
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
}
