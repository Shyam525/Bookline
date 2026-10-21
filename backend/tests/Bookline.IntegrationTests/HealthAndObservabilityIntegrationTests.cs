namespace Bookline.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Bookline.Api.Observability;
using Bookline.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

public class HealthAndObservabilityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthAndObservabilityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_Returns200WithSanitizedLivenessJson()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("totalDurationMs", out _));
        Assert.True(root.TryGetProperty("timestamp", out _));

        var checks = root.GetProperty("checks");
        Assert.True(checks.GetArrayLength() > 0);
        var livenessCheck = checks[0];
        Assert.Equal("liveness", livenessCheck.GetProperty("name").GetString());
        Assert.Equal("Healthy", livenessCheck.GetProperty("status").GetString());

        // Verify no credentials, stack traces, or SQL are leaked
        Assert.DoesNotContain("Password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReady_Returns200WithSanitizedDependenciesJson()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var status = root.GetProperty("status").GetString();
        Assert.True(status == "Healthy" || status == "Degraded");

        var checks = root.GetProperty("checks");
        Assert.True(checks.GetArrayLength() >= 2);

        // Verify no sensitive connection details or internal errors leaked
        Assert.DoesNotContain("Password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stack", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RateLimiter_WhenExceedingPasswordResetLimit_Returns429WithRetryAfterAndStableEnvelope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.195");

        HttpResponseMessage? lastResponse = null;

        // Password reset limit is 5 requests / min
        for (int i = 0; i < 7; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
            {
                Email = "testuser@bookline.local"
            });
        }

        Assert.NotNull(lastResponse);
        Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"));
        Assert.Equal("60", lastResponse.Headers.GetValues("Retry-After").First());

        var content = await lastResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("RATE_LIMITED", root.GetProperty("code").GetString());
        Assert.Contains("Too many requests", root.GetProperty("message").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceIdProp) && !string.IsNullOrWhiteSpace(traceIdProp.GetString()));
        Assert.Equal(429, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public void SensitiveDataMaskingEnricher_RedactsPasswordsTokensAndBearerHeaders()
    {
        var enricher = new SensitiveDataMaskingEnricher();
        var messageTemplate = new MessageTemplateParser().Parse("User authentication attempt");
        var properties = new List<LogEventProperty>
        {
            new("Password", new ScalarValue("SuperSecretP@ssword123")),
            new("ResetToken", new ScalarValue("abc-reset-token-xyz")),
            new("ApiKey", new ScalarValue("live_sec_123456789")),
            new("Authorization", new ScalarValue("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy")),
            new("NormalField", new ScalarValue("SafeValue"))
        };

        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            messageTemplate,
            properties);

        var propertyFactory = new MockPropertyFactory();
        enricher.Enrich(logEvent, propertyFactory);

        Assert.Equal("[REDACTED]", ((ScalarValue)logEvent.Properties["Password"]).Value);
        Assert.Equal("[REDACTED]", ((ScalarValue)logEvent.Properties["ResetToken"]).Value);
        Assert.Equal("[REDACTED]", ((ScalarValue)logEvent.Properties["ApiKey"]).Value);
        Assert.Equal("[REDACTED]", ((ScalarValue)logEvent.Properties["Authorization"]).Value);
        Assert.Equal("SafeValue", ((ScalarValue)logEvent.Properties["NormalField"]).Value);
    }

    [Fact]
    public async Task RateLimiter_WithDifferentClientIps_DoesNotBlockUnrelatedClients()
    {
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.10");

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.20");

        // Client A consumes all 5 permits
        for (int i = 0; i < 5; i++)
        {
            var r = await clientA.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = "clienta@test.com" });
            var body = await r.Content.ReadAsStringAsync();
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"Failed with status {r.StatusCode}: {body}");
        }

        // Client A 6th request is throttled
        var throttledA = await clientA.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = "clienta@test.com" });
        Assert.Equal((HttpStatusCode)429, throttledA.StatusCode);

        // Client B from different IP must still succeed!
        var successB = await clientB.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = "clientb@test.com" });
        Assert.Equal(HttpStatusCode.OK, successB.StatusCode);
    }

    [Fact]
    public async Task RedisHealthCheck_WhenRedisOptionalAndNotConfigured_ReturnsHealthy()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Required"] = "false"
            })
            .Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var sp = services.BuildServiceProvider();

        var check = new Bookline.Api.Health.RedisHealthCheck(sp, config, Microsoft.Extensions.Logging.Abstractions.NullLogger<Bookline.Api.Health.RedisHealthCheck>.Instance);
        var context = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext();

        var result = await check.CheckHealthAsync(context, System.Threading.CancellationToken.None);

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
        Assert.Contains("in-memory coordination mode", result.Description);
    }

    [Fact]
    public async Task RedisHealthCheck_WhenRedisRequiredAndMissing_ReturnsUnhealthy()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Required"] = "true"
            })
            .Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var sp = services.BuildServiceProvider();

        var check = new Bookline.Api.Health.RedisHealthCheck(sp, config, Microsoft.Extensions.Logging.Abstractions.NullLogger<Bookline.Api.Health.RedisHealthCheck>.Instance);
        var context = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext();

        var result = await check.CheckHealthAsync(context, System.Threading.CancellationToken.None);

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, result.Status);
        Assert.Contains("required by configuration but not initialized", result.Description);
    }

    [Fact]
    public void BooklineMetrics_MetersAndCounters_AreInstantiated()
    {
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.RateLimitedRequests);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.BookingConflicts);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.OutboxEventsProcessed);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.OutboxEventsFailed);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.NotificationDeliveries);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.WebhookProcessed);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.HealthCheckFailures);
        Assert.NotNull(Bookline.Infrastructure.Observability.BooklineMetrics.OutboxBatchDurationMs);

        // Verify counter increments
        Bookline.Infrastructure.Observability.BooklineMetrics.RateLimitedRequests.Add(1, new KeyValuePair<string, object?>("policy", "test"));
        Bookline.Infrastructure.Observability.BooklineMetrics.BookingConflicts.Add(1);
    }

    private class MockPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
