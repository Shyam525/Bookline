namespace Bookline.IntegrationTests;

using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Queries;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new InstantJsonConverter());
                });

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BooklineDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbName = "IntegrationTestDb_" + Guid.NewGuid();
            services.AddDbContext<BooklineDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();
            db.Database.EnsureCreated();

            var tenant1 = new Tenant { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Demo Salon", Slug = "acme-salon" };
            var tenant2 = new Tenant { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Other Salon", Slug = "other-salon" };
            db.Tenants.AddRange(tenant1, tenant2);

            var service1 = new Service { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), TenantId = tenant1.Id, Name = "Haircut", DurationMinutes = 45, Price = 50 };
            var service2 = new Service { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), TenantId = tenant2.Id, Name = "Coloring", DurationMinutes = 60, Price = 100 };
            db.Services.AddRange(service1, service2);

            var staff1 = new Staff { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), TenantId = tenant1.Id, Name = "Alex", TimeZoneId = "UTC" };
            var staff2 = new Staff { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), TenantId = tenant2.Id, Name = "Bob", TimeZoneId = "UTC" };
            db.Staff.AddRange(staff1, staff2);

            db.WorkingHours.Add(new WorkingHours { TenantId = tenant1.Id, StaffId = staff1.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
            db.WorkingHours.Add(new WorkingHours { TenantId = tenant2.Id, StaffId = staff2.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });

            db.SaveChanges();
        });
    }
}

public class PublicAvailabilityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PublicAvailabilityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAvailability_WithValidStaffAndServiceForTenant_Returns200OK()
    {
        var staffId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var serviceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act - Request availability for valid staff & service belonging to tenant acme-salon
        var availabilityRes = await _client.GetAsync($"/api/v1/public/availability?staffId={staffId}&serviceId={serviceId}&date=2026-10-30&timezone=UTC&slug=acme-salon");

        // Assert 200 OK
        if (availabilityRes.StatusCode != HttpStatusCode.OK)
        {
            var body = await availabilityRes.Content.ReadAsStringAsync();
            Assert.Fail($"Status: {availabilityRes.StatusCode}, Body: {body}");
        }
        Assert.Equal(HttpStatusCode.OK, availabilityRes.StatusCode);

        using var slotResponse = JsonDocument.Parse(await availabilityRes.Content.ReadAsStringAsync());
        var slots = slotResponse.RootElement.EnumerateArray().ToArray();
        Assert.NotEmpty(slots);
        Assert.True(slots[0].TryGetProperty("start", out _));
        Assert.True(slots[0].TryGetProperty("end", out _));
    }

    [Fact]
    public async Task GetAvailability_WithAnotherTenantsIds_Returns404NotFound()
    {
        // Staff belongs to tenant 1 (acme-salon), but Service belongs to tenant 2 (other-salon)
        var staffId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var otherTenantServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act - Request availability with cross-tenant IDs
        var availabilityRes = await _client.GetAsync($"/api/v1/public/availability?staffId={staffId}&serviceId={otherTenantServiceId}&date=2026-10-30&timezone=UTC&slug=acme-salon");

        // Assert 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, availabilityRes.StatusCode);
    }
}

public class BookingsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookingsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCalendar_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/v1/bookings?fromUtc=2026-10-30T00:00:00Z&toUtc=2026-10-31T00:00:00Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
