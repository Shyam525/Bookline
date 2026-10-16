using System.IO;
using System.Net;
using System.Text.Json;
using Bookline.Api.Middleware;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Section 82: Stable API Error Contract Unit & Integration Verification Suite.
/// Verifies:
/// 1. Stable, documented error contract envelope { code, message, traceId, errors?, extensions? }
/// 2. Required fields: code, message (safe human fallback), traceId (real correlation ID)
/// 3. Standard HTTP status code mapping (400, 401, 403, 404, 409, 429, 500, 503)
/// 4. Correlation ID propagation from context and headers
/// 5. GlobalExceptionMiddleware handling and serialization
/// 6. Safe fallbacks that never leak stack traces or raw internal exception details
/// </summary>
public class ApiErrorContractSection82Tests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ApiErrorResponse_SerializesRequiredContractFieldsCorrectly()
    {
        // Arrange
        var traceId = "test-corr-" + Guid.NewGuid().ToString("N");
        var error = new ApiErrorResponse("SLOT_UNAVAILABLE", "This time is no longer available.", traceId, 409);

        // Act
        var json = JsonSerializer.Serialize(error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        Assert.Contains("\"code\":\"SLOT_UNAVAILABLE\"", json);
        Assert.Contains("\"message\":\"This time is no longer available.\"", json);
        Assert.Contains($"\"traceId\":\"{traceId}\"", json);
        Assert.Contains("\"status\":409", json);

        var deserialized = JsonSerializer.Deserialize<ApiErrorResponse>(json, JsonOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("SLOT_UNAVAILABLE", deserialized.Code);
        Assert.Equal("This time is no longer available.", deserialized.Message);
        Assert.Equal(traceId, deserialized.TraceId);
        Assert.Equal(409, deserialized.Status);
    }

    [Fact]
    public void ValidationFactory_GeneratesStructuredFieldErrors()
    {
        // Arrange
        var traceId = "trace-val-001";
        var fieldErrors = new Dictionary<string, string[]>
        {
            ["email"] = new[] { "Email is required.", "Email must be valid." },
            ["password"] = new[] { "Password must be at least 8 characters." }
        };

        // Act
        var error = ApiErrorResponse.Validation(traceId, fieldErrors);

        // Assert
        Assert.Equal("VALIDATION_FAILED", error.Code);
        Assert.Equal("One or more validation errors occurred.", error.Message);
        Assert.Equal(traceId, error.TraceId);
        Assert.Equal(400, error.Status);
        Assert.NotNull(error.Errors);
        Assert.Equal(2, error.Errors.Count);
        Assert.Equal(2, error.Errors["email"].Length);
        Assert.Single(error.Errors["password"]);
    }

    [Theory]
    [InlineData("RATE_LIMITED", 429)]
    [InlineData("NOT_FOUND", 404)]
    [InlineData("UNAUTHENTICATED", 401)]
    [InlineData("FORBIDDEN", 403)]
    [InlineData("INTERNAL_SERVER_ERROR", 500)]
    [InlineData("SERVICE_UNAVAILABLE", 503)]
    public void FactoryMethods_MapToStandardStatusesAndCodes(string expectedCode, int expectedStatus)
    {
        // Arrange
        var traceId = "trace-" + Guid.NewGuid().ToString("N");

        // Act
        var error = expectedCode switch
        {
            "RATE_LIMITED" => ApiErrorResponse.RateLimited(traceId),
            "NOT_FOUND" => ApiErrorResponse.NotFound("Resource not found.", traceId),
            "UNAUTHENTICATED" => ApiErrorResponse.Unauthenticated(traceId),
            "FORBIDDEN" => ApiErrorResponse.Forbidden(traceId),
            "INTERNAL_SERVER_ERROR" => ApiErrorResponse.InternalError(traceId),
            "SERVICE_UNAVAILABLE" => ApiErrorResponse.ServiceUnavailable(traceId),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedCode))
        };

        // Assert
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedStatus, error.Status);
        Assert.Equal(traceId, error.TraceId);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task Middleware_HandlesValidationException_WithCamelCaseErrorsAnd400()
    {
        // Arrange
        var expectedTraceId = "req-validation-400";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var failures = new List<ValidationFailure>
        {
            new("CustomerEmail", "Invalid customer email address."),
            new("CustomerEmail", "Customer email cannot be empty."),
            new("SlotStartUtc", "Start time must be in future.")
        };
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new ValidationException(failures),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("VALIDATION_FAILED", envelope.Code);
        Assert.Equal("One or more validation errors occurred.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
        Assert.NotNull(envelope.Errors);
        Assert.True(envelope.Errors.ContainsKey("customerEmail"));
        Assert.True(envelope.Errors.ContainsKey("slotStartUtc"));
        Assert.Equal(2, envelope.Errors["customerEmail"].Length);
    }

    [Fact]
    public async Task Middleware_HandlesBookingConflictException_With409AndSlotUnavailable()
    {
        // Arrange
        var expectedTraceId = "req-booking-conflict-409";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var nearestSlots = new List<Slot>
        {
            new(NodaTime.Instant.FromDateTimeUtc(now.AddHours(1)), NodaTime.Instant.FromDateTimeUtc(now.AddHours(2))),
            new(NodaTime.Instant.FromDateTimeUtc(now.AddHours(3)), NodaTime.Instant.FromDateTimeUtc(now.AddHours(4)))
        };

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new BookingConflictException(nearestSlots),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("SLOT_UNAVAILABLE", envelope.Code);
        Assert.Equal("This time is no longer available.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
        Assert.NotNull(envelope.Extensions);
        Assert.True(envelope.Extensions.ContainsKey("nearestFreeSlots"));
    }

    [Fact]
    public async Task Middleware_HandlesConflictException_With409AndCustomCode()
    {
        // Arrange
        var expectedTraceId = "req-inv-conflict-409";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw ConflictException.Inventory("Insufficient inventory for Shampoo bottle."),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("INVENTORY_CONFLICT", envelope.Code);
        Assert.Equal("Insufficient inventory for Shampoo bottle.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    [Fact]
    public async Task Middleware_HandlesNotFoundException_With404AndNotFoundCode()
    {
        // Arrange
        var expectedTraceId = "req-not-found-404";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new NotFoundException("Provider", "provider-abc-123"),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("NOT_FOUND", envelope.Code);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    [Fact]
    public async Task Middleware_HandlesUnauthenticatedException_With401()
    {
        // Arrange
        var expectedTraceId = "req-unauth-401";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new UnauthenticatedException(),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("UNAUTHENTICATED", envelope.Code);
        Assert.Equal("Authentication is required to access this resource.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    [Fact]
    public async Task Middleware_HandlesForbiddenAccessException_With403()
    {
        // Arrange
        var expectedTraceId = "req-forbidden-403";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new ForbiddenAccessException("You lack access to this tenant."),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("FORBIDDEN", envelope.Code);
        Assert.Equal("You lack access to this tenant.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    [Fact]
    public async Task Middleware_HandlesServiceUnavailableException_With503()
    {
        // Arrange
        var expectedTraceId = "req-unavailable-503";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new ServiceUnavailableException("Payment gateway is temporarily down."),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("SERVICE_UNAVAILABLE", envelope.Code);
        Assert.Equal("Payment gateway is temporarily down.", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    [Fact]
    public async Task Middleware_HandlesUnhandledException_With500AndSafeFallback_NeverLeaksStackTrace()
    {
        // Arrange
        var expectedTraceId = "req-crash-500";
        var context = CreateHttpContextWithCorrelationId(expectedTraceId);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new NullReferenceException("Fatal internal pointer dereference in private memory!"),
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        var envelope = await ReadResponseEnvelopeAsync(context);

        Assert.Equal("INTERNAL_SERVER_ERROR", envelope.Code);
        // Ensure safe fallback message: must NOT leak the raw internal exception or stack trace
        Assert.Equal("An unexpected application error occurred.", envelope.Message);
        Assert.DoesNotContain("Fatal internal pointer dereference", envelope.Message);
        Assert.Equal(expectedTraceId, envelope.TraceId);
    }

    private static HttpContext CreateHttpContextWithCorrelationId(string correlationId)
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ApiErrorResponse> ReadResponseEnvelopeAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.False(string.IsNullOrWhiteSpace(body), "Response body should not be empty");
        var envelope = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
        Assert.NotNull(envelope);
        return envelope!;
    }
}
