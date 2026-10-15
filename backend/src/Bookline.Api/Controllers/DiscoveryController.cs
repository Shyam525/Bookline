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
[Route("api/discovery")]
[EnableRateLimiting("discovery-limit")]
public class DiscoveryController : ControllerBase
{
    private readonly IProviderSearchService _searchService;
    private readonly IProviderDiscoveryService _discoveryEngine;
    private readonly ILocationDiscoveryService _locationDiscoveryService;
    private readonly IGeocodingProvider _geocodingProvider;
    private readonly IApplicationDbContext _dbContext;

    public DiscoveryController(
        IProviderSearchService searchService,
        IProviderDiscoveryService discoveryEngine,
        ILocationDiscoveryService locationDiscoveryService,
        IGeocodingProvider geocodingProvider,
        IApplicationDbContext dbContext)
    {
        _searchService = searchService;
        _discoveryEngine = discoveryEngine;
        _locationDiscoveryService = locationDiscoveryService;
        _geocodingProvider = geocodingProvider;
        _dbContext = dbContext;
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookline.Application.Common.Models.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProviders(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? service,
        [FromQuery] string? city,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radius,
        [FromQuery] double? radiusMeters,
        [FromQuery] string? bounds,
        [FromQuery] double? swLat,
        [FromQuery] double? swLng,
        [FromQuery] double? neLat,
        [FromQuery] double? neLng,
        [FromQuery] double? minRating,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? availability,
        [FromQuery] string? availabilityDate,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate geographic coordinate ranges
        if (lat.HasValue && (lat.Value < -90.0 || lat.Value > 90.0))
        {
            return BadRequest(new Bookline.Application.Common.Models.ApiErrorResponse(
                "INVALID_SEARCH_LOCATION",
                "Latitude must be between -90.0 and +90.0 degrees.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        if (lng.HasValue && (lng.Value < -180.0 || lng.Value > 180.0))
        {
            return BadRequest(new Bookline.Application.Common.Models.ApiErrorResponse(
                "INVALID_SEARCH_LOCATION",
                "Longitude must be between -180.0 and +180.0 degrees.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        // 2. Parse bounds string if provided (format: "swLat,swLng,neLat,neLng")
        if (!string.IsNullOrWhiteSpace(bounds))
        {
            var parts = bounds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 &&
                double.TryParse(parts[0], out var parsedSwLat) &&
                double.TryParse(parts[1], out var parsedSwLng) &&
                double.TryParse(parts[2], out var parsedNeLat) &&
                double.TryParse(parts[3], out var parsedNeLng))
            {
                swLat = parsedSwLat;
                swLng = parsedSwLng;
                neLat = parsedNeLat;
                neLng = parsedNeLng;
            }
            else
            {
                return BadRequest(new Bookline.Application.Common.Models.ApiErrorResponse(
                    "INVALID_BOUNDING_BOX",
                    "Bounds must be formatted as 'swLat,swLng,neLat,neLng'.",
                    HttpContext.TraceIdentifier,
                    400
                ));
            }
        }

        // 3. Validate bounding box coordinates
        if (swLat.HasValue && neLat.HasValue && swLat.Value > neLat.Value)
        {
            return BadRequest(new Bookline.Application.Common.Models.ApiErrorResponse(
                "INVALID_BOUNDING_BOX",
                "Southwest latitude cannot exceed northeast latitude.",
                HttpContext.TraceIdentifier,
                400
            ));
        }

        // 4. Validate radius
        double? effectiveRadiusKm = radius;
        if (radiusMeters.HasValue)
        {
            if (radiusMeters.Value <= 0)
            {
                return BadRequest(new Bookline.Application.Common.Models.ApiErrorResponse(
                    "INVALID_RADIUS",
                    "Radius must be a positive number.",
                    HttpContext.TraceIdentifier,
                    400
                ));
            }
            effectiveRadiusKm = radiusMeters.Value / 1000.0;
        }

        var searchQuery = new ProviderSearchQuery(
            Query: q,
            Category: category,
            Service: service,
            City: city,
            Latitude: lat,
            Longitude: lng,
            RadiusKm: effectiveRadiusKm,
            SwLat: swLat,
            SwLng: swLng,
            NeLat: neLat,
            NeLng: neLng,
            MinRating: minRating,
            MaxPrice: maxPrice,
            Availability: availability ?? availabilityDate,
            Sort: sort,
            Page: Math.Max(1, page),
            PageSize: Math.Clamp(pageSize, 1, 50)
        );

        var response = await _searchService.SearchProvidersAsync(searchQuery, cancellationToken);
        return Ok(response);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Discover(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? service,
        [FromQuery] string? placeId,
        [FromQuery] string? city,
        [FromQuery] string? area,
        [FromQuery] string? postcode,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radius,
        [FromQuery] double? swLat,
        [FromQuery] double? swLng,
        [FromQuery] double? neLat,
        [FromQuery] double? neLng,
        [FromQuery] double? minRating,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] bool includeMarkers = false,
        CancellationToken cancellationToken = default)
    {
        ViewportBounds? viewport = null;
        if (swLat.HasValue && swLng.HasValue && neLat.HasValue && neLng.HasValue)
        {
            viewport = new ViewportBounds(swLat.Value, swLng.Value, neLat.Value, neLng.Value);
        }

        PlaceSuggestionDto? selectedPlace = null;
        if (!string.IsNullOrWhiteSpace(placeId))
        {
            selectedPlace = await _locationDiscoveryService.GetPlaceByIdAsync(placeId, cancellationToken);
        }

        double? radiusMeters = radius;
        if (radiusMeters.HasValue && radiusMeters.Value > 0 && radiusMeters.Value <= 500.0)
        {
            radiusMeters *= 1000.0; // Automatically convert km to meters
        }

        var discoverySortMode = ParseSortMode(sort);

        var query = new DiscoveryQuery(
            QueryText: q,
            CategorySlug: category,
            ServiceSlug: service,
            SelectedPlace: selectedPlace,
            Latitude: lat,
            Longitude: lng,
            RadiusMeters: radiusMeters,
            ViewportBounds: viewport,
            MinimumRating: minRating,
            MaximumPrice: maxPrice,
            AvailabilityDate: null,
            SortMode: discoverySortMode,
            Page: page,
            PageSize: pageSize,
            IncludeMarkers: includeMarkers
        );

        var response = await _discoveryEngine.DiscoverProvidersAsync(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("places")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPlaces([FromQuery] string? q, CancellationToken cancellationToken = default)
    {
        var places = await _locationDiscoveryService.SearchPlacesAsync(q ?? string.Empty, cancellationToken);
        return Ok(places);
    }

    [HttpPost("places/resolve")]
    [AllowAnonymous]
    public async Task<IActionResult> ResolveLocation([FromBody] LocationResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _locationDiscoveryService.ResolveLocationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("places/cities")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeaturedDiscoveryCities(CancellationToken cancellationToken = default)
    {
        var cities = await _locationDiscoveryService.GetFeaturedCitiesAsync(cancellationToken);
        return Ok(cities);
    }

    private static DiscoverySortMode ParseSortMode(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return DiscoverySortMode.Recommended;
        return sort.Trim().ToLowerInvariant() switch
        {
            "nearest" => DiscoverySortMode.Nearest,
            "top rated" or "toprated" or "top_rated" => DiscoverySortMode.TopRated,
            "most reviewed" or "mostreviewed" or "most_reviewed" => DiscoverySortMode.MostReviewed,
            "earliest available" or "earliest" or "earliestavailable" or "earliest_available" => DiscoverySortMode.EarliestAvailable,
            "lowest price" or "lowestprice" or "lowest_price" => DiscoverySortMode.LowestPrice,
            "highest price" or "highestprice" or "highest_price" => DiscoverySortMode.HighestPrice,
            _ => DiscoverySortMode.Recommended
        };
    }

    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPublicServices(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = from s in _dbContext.Services.IgnoreQueryFilters()
                        join t in _dbContext.Tenants.IgnoreQueryFilters() on s.TenantId equals t.Id
                        where s.IsActive && !s.IsArchived && s.IsOnlineBookingEnabled && t.IsActive && t.IsPublished
                        select new { s, t };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQuery = baseQuery.Where(x =>
                x.s.Name.ToLower().Contains(term) ||
                (x.s.Description != null && x.s.Description.ToLower().Contains(term)) ||
                x.t.Name.ToLower().Contains(term) ||
                x.t.City.ToLower().Contains(term));
        }

        var results = await baseQuery
            .OrderBy(x => x.s.Name)
            .Take(Math.Clamp(limit, 1, 20))
            .Select(x => new
            {
                Id = x.s.Id,
                Name = x.s.Name,
                Description = x.s.Description,
                DurationMinutes = x.s.DurationMinutes,
                Price = x.s.Price,
                Currency = x.t.Currency ?? "INR",
                ProviderName = x.t.Name,
                ProviderSlug = x.t.Slug,
                ProviderCity = x.t.City,
                ProviderCategory = x.t.Category,
                IsOnlineBookingEnabled = x.s.IsOnlineBookingEnabled
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
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
            .Include(p => p.Images)
            .Where(p => p.TenantId == tenant.Id && p.IsActive && p.IsPublished)
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
            p.IsInStock,
            p.Slug,
            p.IsPurchasableOnline,
            p.AvailableQuantity,
            p.CategoryId,
            p.Images?.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
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
