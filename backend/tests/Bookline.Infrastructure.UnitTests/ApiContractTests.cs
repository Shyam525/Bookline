using System.Text.Json;
using Bookline.Application.Common.Models;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

/// <summary>
/// Unit tests verifying Section 113: API Contract & Stable Error Envelope
/// Expected shape:
/// {
///   "code": "SLOT_UNAVAILABLE",
///   "message": "That time was just booked. Please choose another time.",
///   "traceId": "..."
/// }
/// </summary>
public class ApiContractTests
{
    [Fact]
    public void Section113_ApiErrorResponse_SerializesExpectedContractFormat()
    {
        // Arrange
        var traceId = Guid.NewGuid().ToString("N");
        var error = new ApiErrorResponse(
            code: "SLOT_UNAVAILABLE",
            message: "That time was just booked. Please choose another time.",
            traceId: traceId,
            status: 409
        );

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Act
        var json = JsonSerializer.Serialize(error, options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("code", out var codeElem));
        Assert.Equal("SLOT_UNAVAILABLE", codeElem.GetString());

        Assert.True(root.TryGetProperty("message", out var msgElem));
        Assert.Equal("That time was just booked. Please choose another time.", msgElem.GetString());

        Assert.True(root.TryGetProperty("traceId", out var traceElem));
        Assert.Equal(traceId, traceElem.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElem));
        Assert.Equal(409, statusElem.GetInt32());
    }

    [Fact]
    public void Section113_ValidationErrors_AreAttachedCorrectly()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]>
        {
            { "email", new[] { "Email is required." } },
            { "phone", new[] { "Phone format is invalid." } }
        };

        var error = new ApiErrorResponse(
            code: "VALIDATION_ERROR",
            message: "One or more validation errors occurred.",
            traceId: "test-trace-id",
            status: 400
        )
        {
            Errors = validationErrors
        };

        var json = JsonSerializer.Serialize(error, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("VALIDATION_ERROR", root.GetProperty("code").GetString());
        Assert.True(root.TryGetProperty("errors", out var errorsProp));
        Assert.True(errorsProp.TryGetProperty("email", out var emailProp));
        Assert.Equal("Email is required.", emailProp[0].GetString());
    }
}
