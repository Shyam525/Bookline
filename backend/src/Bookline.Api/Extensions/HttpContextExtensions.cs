using Bookline.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Retrieves the established request correlation ID or falls back to HttpContext.TraceIdentifier.
    /// </summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue("CorrelationId", out var cid) && cid is string cStr && !string.IsNullOrWhiteSpace(cStr))
        {
            return cStr;
        }

        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }

        return context.TraceIdentifier;
    }

    public static string GetCorrelationId(this ControllerBase controller) =>
        controller.HttpContext.GetCorrelationId();

    public static BadRequestObjectResult ApiBadRequest(this ControllerBase controller, string message, string code = "BAD_REQUEST")
    {
        var traceId = controller.GetCorrelationId();
        return new BadRequestObjectResult(new ApiErrorResponse(code, message, traceId, StatusCodes.Status400BadRequest));
    }

    public static BadRequestObjectResult ApiValidation(this ControllerBase controller, IDictionary<string, string[]> errors, string message = "One or more validation errors occurred.")
    {
        var traceId = controller.GetCorrelationId();
        return new BadRequestObjectResult(ApiErrorResponse.Validation(traceId, errors, message));
    }

    public static NotFoundObjectResult ApiNotFound(this ControllerBase controller, string message = "The requested resource was not found.", string code = "NOT_FOUND")
    {
        var traceId = controller.GetCorrelationId();
        return new NotFoundObjectResult(new ApiErrorResponse(code, message, traceId, StatusCodes.Status404NotFound));
    }

    public static UnauthorizedObjectResult ApiUnauthorized(this ControllerBase controller, string message = "Authentication is required to access this resource.")
    {
        var traceId = controller.GetCorrelationId();
        return new UnauthorizedObjectResult(ApiErrorResponse.Unauthenticated(traceId, message));
    }

    public static ObjectResult ApiForbidden(this ControllerBase controller, string message = "You do not have permission to access this resource.")
    {
        var traceId = controller.GetCorrelationId();
        return new ObjectResult(ApiErrorResponse.Forbidden(traceId, message)) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public static ConflictObjectResult ApiConflict(this ControllerBase controller, string code, string message, IDictionary<string, object?>? extensions = null)
    {
        var traceId = controller.GetCorrelationId();
        return new ConflictObjectResult(ApiErrorResponse.Conflict(code, message, traceId, extensions));
    }
}
