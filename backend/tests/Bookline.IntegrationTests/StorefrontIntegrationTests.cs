namespace Bookline.IntegrationTests;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class StorefrontIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StorefrontIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStorefront_WithValidPublishedSlug_Returns200OKAndPublicDto()
    {
        // "acme-salon" was seeded in CustomWebApplicationFactory
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant != null)
            {
                tenant.IsActive = true;
                tenant.IsPublished = true;
                tenant.VerificationStatus = VerificationStatus.Verified;
                tenant.City = "New York";
                await db.SaveChangesAsync();
            }
        }

        var response = await _client.GetAsync("/api/v1/public/businesses/acme-salon");

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Failed with {response.StatusCode}: {content}");
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("provider", out var providerProp));
        Assert.Equal("acme-salon", providerProp.GetProperty("slug").GetString());
        Assert.Equal("Demo Salon", providerProp.GetProperty("name").GetString());

        // Assert sensitive financial data is not leaked in public storefront
        Assert.False(providerProp.TryGetProperty("commissionRatePercentage", out _));
        Assert.False(providerProp.TryGetProperty("availablePayoutBalance", out _));
    }

    [Fact]
    public async Task GetStorefront_WithCanonicalCityAlias_ResolvesSuccessfully()
    {
        var response = await _client.GetAsync("/api/v1/public/businesses/acme-salon-new-york");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("acme-salon", doc.RootElement.GetProperty("provider").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task GetStorefront_WithUnpublishedOrSuspendedBusiness_Returns404NotFound()
    {
        // Seed an unpublished tenant
        var unpublishedId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();
            db.Tenants.Add(new Tenant
            {
                Id = unpublishedId,
                Name = "Hidden Studio",
                Slug = "hidden-studio",
                IsActive = true,
                IsPublished = false, // UNPUBLISHED
                VerificationStatus = VerificationStatus.Pending,
                City = "Mumbai"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/public/businesses/hidden-studio");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStorefront_WithInvalidNonExistentSlug_Returns404NotFound()
    {
        var response = await _client.GetAsync("/api/v1/public/businesses/non-existent-salon-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
