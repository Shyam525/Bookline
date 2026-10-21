using System.Net;
using System.Text.Json;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Models;
using FluentValidation;

namespace Bookline.Api.Middleware;

/// <summary>
/// Global exception handling middleware ensuring Section 113 stable API error contract:
/// {
///   "code": "SLOT_UNAVAILABLE",
///   "message": "That time was just booked. Please choose another time.",
///   "traceId": "..."
/// }
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request processing. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        var traceId = context.Items.TryGetValue("CorrelationId", out var cid) && cid is string cStr && !string.IsNullOrWhiteSpace(cStr)
            ? cStr
            : context.TraceIdentifier;

        var errorResponse = new ApiErrorResponse
        {
            TraceId = traceId
        };

        switch (exception)
        {
            case ValidationException valEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "VALIDATION_FAILED";
                errorResponse.Message = "One or more validation errors occurred.";
                errorResponse.Errors = valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Status = (int)HttpStatusCode.NotFound;
                errorResponse.Code = "NOT_FOUND";
                errorResponse.Message = notFoundEx.Message;
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Status = (int)HttpStatusCode.NotFound;
                errorResponse.Code = "NOT_FOUND";
                errorResponse.Message = "The requested resource was not found.";
                break;

            case BookingConflictException conflictEx:
                Bookline.Infrastructure.Observability.BooklineMetrics.BookingConflicts.Add(1);
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Status = (int)HttpStatusCode.Conflict;
                errorResponse.Code = "SLOT_UNAVAILABLE";
                errorResponse.Message = "This time is no longer available.";
                errorResponse.Extensions = new Dictionary<string, object?>
                {
                    ["nearestFreeSlots"] = conflictEx.NearestFreeSlots
                };
                break;

            case ConflictException confEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Status = (int)HttpStatusCode.Conflict;
                errorResponse.Code = confEx.Code;
                errorResponse.Message = confEx.Message;
                errorResponse.Extensions = confEx.Extensions;
                break;

            case IdempotencyConflictException idempEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Status = (int)HttpStatusCode.Conflict;
                errorResponse.Code = "IDEMPOTENCY_CONFLICT";
                errorResponse.Message = idempEx.Message;
                errorResponse.Extensions = new Dictionary<string, object?>
                {
                    ["idempotencyKey"] = idempEx.IdempotencyKey
                };
                break;

            case UnauthenticatedException unauthEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                errorResponse.Code = unauthEx.Code;
                errorResponse.Message = unauthEx.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                errorResponse.Code = "UNAUTHENTICATED";
                errorResponse.Message = "Authentication is required to access this resource.";
                break;

            case ForbiddenAccessException forbEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Status = (int)HttpStatusCode.Forbidden;
                errorResponse.Code = "FORBIDDEN";
                errorResponse.Message = forbEx.Message;
                break;

            case ServiceUnavailableException srvEx:
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                errorResponse.Status = (int)HttpStatusCode.ServiceUnavailable;
                errorResponse.Code = srvEx.Code;
                errorResponse.Message = srvEx.Message;
                break;

            case Bookline.Domain.Exceptions.DomainException domainEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "BAD_REQUEST";
                errorResponse.Message = domainEx.Message;
                break;

            case InvalidOperationException invEx when !invEx.Message.Contains("PipeWriter"):
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "BAD_REQUEST";
                errorResponse.Message = invEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                errorResponse.Code = "INTERNAL_SERVER_ERROR";
                errorResponse.Message = "An unexpected application error occurred.";
                break;
        }

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
