using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/discovery")]
[EnableRateLimiting("discovery-limit")]
public class DiscoveryController : ControllerBase
{
    private readonly IProviderSearchService _searchService;
    private readonly IGeocodingProvider _geocodingProvider;
    private readonly BooklineDbContext _dbContext;

    public DiscoveryController(
        IProviderSearchService searchService,
        IGeocodingProvider geocodingProvider,
        BooklineDbContext dbContext)
    {
        _searchService = searchService;
        _geocodingProvider = geocodingProvider;
        _dbContext = dbContext;
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProviders(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? service,
        [FromQuery] string? city,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radius,
        [FromQuery] double? swLat,
        [FromQuery] double? swLng,
        [FromQuery] double? neLat,
        [FromQuery] double? neLng,
        [FromQuery] double? minRating,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? availability,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var searchQuery = new ProviderSearchQuery(
            Query: q,
            Category: category,
            Service: service,
            City: city,
            Latitude: lat,
            Longitude: lng,
            RadiusKm: radius,
            SwLat: swLat,
            SwLng: swLng,
            NeLat: neLat,
            NeLng: neLng,
            MinRating: minRating,
            MaxPrice: maxPrice,
            Availability: availability,
            Sort: sort,
            Page: page,
            PageSize: pageSize
        );

        var response = await _searchService.SearchProvidersAsync(searchQuery, cancellationToken);
        return Ok(response);
    }

    [HttpGet("providers/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProviderStorefront(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive && t.IsPublished, cancellationToken);

        if (tenant == null)
        {
            return NotFound(new { Message = "Provider storefront not found." });
        }

        var locations = await _dbContext.Locations.IgnoreQueryFilters()
            .Where(l => l.TenantId == tenant.Id && l.IsActive && !l.IsArchived)
            .ToListAsync(cancellationToken);

        var categories = await _dbContext.ServiceCategories.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        var services = await _dbContext.Services.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenant.Id && s.IsActive && !s.IsArchived)
            .ToListAsync(cancellationToken);

        var products = await _dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenant.Id && p.IsActive && p.IsPurchasableOnline)
            .ToListAsync(cancellationToken);

        var staff = await _dbContext.Staff.IgnoreQueryFilters()
            .Where(st => st.TenantId == tenant.Id && st.IsActive && !st.IsArchived)
            .Select(st => new
            {
                st.Id,
                st.Name,
                st.Title,
                st.Bio,
                st.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        var reviews = await _dbContext.Reviews.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenant.Id && r.ModerationStatus == ModerationStatus.Approved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var locationDtos = locations.Select(l => new PublicLocationDto(
            l.Id,
            l.Name,
            l.Address,
            l.City,
            l.State,
            l.PostalCode,
            l.Latitude,
            l.Longitude,
            l.Phone,
            l.IsActive
        )).ToList();

        var categoryDtos = categories.Select(c => new PublicServiceCategoryDto(
            c.Id,
            c.Name,
            c.Description
        )).ToList();

        var serviceDtos = services.Select(s => new PublicServiceDto(
            s.Id,
            s.CategoryId,
            s.Name,
            s.Description,
            s.DurationMinutes,
            s.BufferMinutes,
            s.Price,
            tenant.Currency ?? "USD",
            s.IsOnlineBookingEnabled
        )).ToList();

        var productDtos = products.Select(p => new PublicProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            tenant.Currency ?? "USD",
            p.StockQuantity,
            p.Sku,
            p.ImageUrl,
            p.StockQuantity > 0
        )).ToList();

        var staffDtos = staff.Select(st => new PublicStaffDto(
            st.Id,
            st.Name,
            st.Title,
            st.Bio,
            st.AvatarUrl
        )).ToList();

        var reviewDtos = reviews.Select(r => new PublicReviewDto(
            r.Id,
            r.CustomerName,
            r.Rating,
            r.Comment,
            r.CreatedAtUtc
        )).ToList();

        var providerDto = new PublicProviderProfileDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Category,
            tenant.BusinessType,
            tenant.Description,
            tenant.Address,
            tenant.City,
            tenant.State,
            tenant.PostalCode,
            tenant.Latitude,
            tenant.Longitude,
            tenant.Phone,
            tenant.Website,
            tenant.AverageRating,
            tenant.ReviewCount,
            tenant.LogoUrl,
            tenant.CoverImageUrl,
            !string.IsNullOrWhiteSpace(tenant.GalleryImagesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<string[]>(tenant.GalleryImagesJson) ?? Array.Empty<string>()
                : Array.Empty<string>(),
            tenant.VerificationStatus == VerificationStatus.Verified,
            tenant.DepositType.ToString(),
            tenant.DepositAmount,
            tenant.Currency ?? "USD",
            tenant.HoldDurationMinutes,
            tenant.MinimumNoticeHours
        );

        return Ok(new PublicStorefrontResponse(
            providerDto,
            locationDtos,
            categoryDtos,
            serviceDtos,
            productDtos,
            staffDtos,
            reviewDtos
        ));
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var categoryCounts = await _dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => t.IsActive && t.IsPublished)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var predefined = new[]
        {
            new { Category = "Beauty & Wellness", Icon = "sparkles", Description = "Hair, nails, skin, aesthetics & spas" },
            new { Category = "Healthcare & Clinics", Icon = "activity", Description = "Dental, dermatology, therapy & doctors" },
            new { Category = "Fitness & Training", Icon = "dumbbell", Description = "Personal trainers, yoga & pilates studios" },
            new { Category = "Professional Services", Icon = "briefcase", Description = "Consulting, legal & financial advisory" },
            new { Category = "Photography & Media", Icon = "camera", Description = "Portraits, commercial & event coverage" },
            new { Category = "Education & Coaching", Icon = "book-open", Description = "Language, music lessons & tutoring" }
        };

        var result = predefined.Select(p => new
        {
            Name = p.Category,
            Icon = p.Icon,
            Description = p.Description,
            Count = categoryCounts.FirstOrDefault(c => c.Category.Contains(p.Category.Split(' ')[0], StringComparison.OrdinalIgnoreCase))?.Count ?? 4
        }).ToList();

        return Ok(result);
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCities(CancellationToken cancellationToken = default)
    {
        var cities = new[]
        {
            new { Name = "Ahmedabad", State = "Gujarat", Lat = 23.0225, Lng = 72.5714, ProviderCount = 14 },
            new { Name = "Mumbai", State = "Maharashtra", Lat = 19.0760, Lng = 72.8777, ProviderCount = 22 },
            new { Name = "Bangalore", State = "Karnataka", Lat = 12.9716, Lng = 77.5946, ProviderCount = 18 },
            new { Name = "Surat", State = "Gujarat", Lat = 21.1702, Lng = 72.8311, ProviderCount = 9 },
            new { Name = "Rajkot", State = "Gujarat", Lat = 22.3039, Lng = 70.8022, ProviderCount = 8 }
        };

        return Ok(cities);
    }

    [HttpPost("geocode")]
    [AllowAnonymous]
    public async Task<IActionResult> Geocode([FromBody] GeocodeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _geocodingProvider.GeocodeAsync(request.Query, cancellationToken);
        if (result == null) return NotFound(new { Message = "Location could not be geocoded." });
        return Ok(result);
    }

    [HttpPost("reverse-geocode")]
    [AllowAnonymous]
    public async Task<IActionResult> ReverseGeocode([FromBody] ReverseGeocodeRequest request, CancellationToken cancellationToken = default)
    {
        var city = await _geocodingProvider.ReverseGeocodeAsync(request.Latitude, request.Longitude, cancellationToken);
        return Ok(new { City = city });
    }
}

public record GeocodeRequest(string Query);
public record ReverseGeocodeRequest(double Latitude, double Longitude);
