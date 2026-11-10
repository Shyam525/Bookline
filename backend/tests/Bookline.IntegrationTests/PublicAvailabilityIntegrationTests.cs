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
    private static readonly object _initLock = new();
    private static bool _initialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", PostgreSqlTestDatabaseFixture.ConnectionString);

        builder.ConfigureServices(services =>
        {
            lock (_initLock)
            {
                if (_initialized) return;

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();
                db.Database.Migrate();

                var tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
                var tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

                var existingTenant1 = db.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.Id == tenant1Id);
                if (existingTenant1 == null)
                {
                    db.Tenants.Add(new Tenant
                    {
                        Id = tenant1Id,
                        Name = "Demo Salon",
                        Slug = "acme-salon",
                        IsActive = true,
                        IsPublished = true,
                        VerificationStatus = Bookline.Domain.Enums.VerificationStatus.Verified,
                        City = "New York",
                        IsDemo = true
                    });
                }
                else
                {
                    existingTenant1.Name = "Demo Salon";
                    existingTenant1.Slug = "acme-salon";
                    existingTenant1.IsActive = true;
                    existingTenant1.IsPublished = true;
                    existingTenant1.VerificationStatus = Bookline.Domain.Enums.VerificationStatus.Verified;
                    existingTenant1.City = "New York";
                    existingTenant1.IsDemo = true;
                }

                var existingTenant2 = db.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.Id == tenant2Id);
                if (existingTenant2 == null)
                {
                    db.Tenants.Add(new Tenant
                    {
                        Id = tenant2Id,
                        Name = "Other Salon",
                        Slug = "other-salon",
                        IsActive = true,
                        IsPublished = true,
                        VerificationStatus = Bookline.Domain.Enums.VerificationStatus.Verified,
                        IsDemo = true
                    });
                }
                else
                {
                    existingTenant2.Name = "Other Salon";
                    existingTenant2.Slug = "other-salon";
                    existingTenant2.IsActive = true;
                    existingTenant2.IsPublished = true;
                    existingTenant2.VerificationStatus = Bookline.Domain.Enums.VerificationStatus.Verified;
                    existingTenant2.IsDemo = true;
                }

                var cat1Id = Guid.Parse("55555555-5555-5555-5555-555555555551");
                var cat2Id = Guid.Parse("55555555-5555-5555-5555-555555555552");

                var existingCat1 = db.ServiceCategories.IgnoreQueryFilters().FirstOrDefault(c => c.Id == cat1Id);
                if (existingCat1 == null)
                {
                    db.ServiceCategories.Add(new ServiceCategory { Id = cat1Id, TenantId = tenant1Id, Name = "Hair Services" });
                }

                var existingCat2 = db.ServiceCategories.IgnoreQueryFilters().FirstOrDefault(c => c.Id == cat2Id);
                if (existingCat2 == null)
                {
                    db.ServiceCategories.Add(new ServiceCategory { Id = cat2Id, TenantId = tenant2Id, Name = "Hair Services" });
                }

                var service1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
                if (!db.Services.IgnoreQueryFilters().Any(s => s.Id == service1Id))
                {
                    db.Services.Add(new Service { Id = service1Id, TenantId = tenant1Id, CategoryId = cat1Id, Name = "Haircut", DurationMinutes = 45, Price = 50, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
                }

                var service2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
                if (!db.Services.IgnoreQueryFilters().Any(s => s.Id == service2Id))
                {
                    db.Services.Add(new Service { Id = service2Id, TenantId = tenant2Id, CategoryId = cat2Id, Name = "Coloring", DurationMinutes = 60, Price = 100, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
                }

                var staff1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
                if (!db.Staff.IgnoreQueryFilters().Any(s => s.Id == staff1Id))
                {
                    db.Staff.Add(new Staff { Id = staff1Id, TenantId = tenant1Id, Name = "Alex", TimeZoneId = "UTC", IsActive = true });
                }

                var staff2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
                if (!db.Staff.IgnoreQueryFilters().Any(s => s.Id == staff2Id))
                {
                    db.Staff.Add(new Staff { Id = staff2Id, TenantId = tenant2Id, Name = "Bob", TimeZoneId = "UTC", IsActive = true });
                }

                if (!db.StaffServices.IgnoreQueryFilters().Any(ss => ss.StaffId == staff1Id && ss.ServiceId == service1Id))
                {
                    db.StaffServices.Add(new StaffService { TenantId = tenant1Id, StaffId = staff1Id, ServiceId = service1Id });
                }

                if (!db.StaffServices.IgnoreQueryFilters().Any(ss => ss.StaffId == staff2Id && ss.ServiceId == service2Id))
                {
                    db.StaffServices.Add(new StaffService { TenantId = tenant2Id, StaffId = staff2Id, ServiceId = service2Id });
                }

                if (!db.WorkingHours.IgnoreQueryFilters().Any(w => w.TenantId == tenant1Id && w.StaffId == staff1Id))
                {
                    for (int d = 0; d < 7; d++)
                    {
                        db.WorkingHours.Add(new WorkingHours { TenantId = tenant1Id, StaffId = staff1Id, DayOfWeek = (DayOfWeek)d, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
                    }
                }

                if (!db.WorkingHours.IgnoreQueryFilters().Any(w => w.TenantId == tenant2Id && w.StaffId == staff2Id))
                {
                    for (int d = 0; d < 7; d++)
                    {
                        db.WorkingHours.Add(new WorkingHours { TenantId = tenant2Id, StaffId = staff2Id, DayOfWeek = (DayOfWeek)d, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
                    }
                }

                db.SaveChanges();
                _initialized = true;
            }
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
        var futureDate = DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd");

        // Act - Request availability for valid staff & service belonging to tenant acme-salon
        var availabilityRes = await _client.GetAsync($"/api/v1/public/availability?staffId={staffId}&serviceId={serviceId}&date={futureDate}&timezone=UTC&slug=acme-salon");

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
        var futureDate = DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd");

        // Act - Request availability with cross-tenant IDs
        var availabilityRes = await _client.GetAsync($"/api/v1/public/availability?staffId={staffId}&serviceId={otherTenantServiceId}&date={futureDate}&timezone=UTC&slug=acme-salon");

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
