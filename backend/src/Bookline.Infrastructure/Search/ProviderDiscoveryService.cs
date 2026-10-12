using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;
using Bookline.Application.Discovery.Validators;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Infrastructure.Search;

public class ProviderDiscoveryService : IProviderDiscoveryService
{
    private readonly BooklineDbContext _dbContext;
    private readonly ILocationDiscoveryService _locationService;
    private readonly IProviderRankingService _rankingService;
    private readonly ISearchIntentService _intentService;
    private readonly DiscoveryQueryValidator _validator = new();

    public ProviderDiscoveryService(
        BooklineDbContext dbContext,
        ILocationDiscoveryService locationService,
        IProviderRankingService rankingService,
        ISearchIntentService intentService)
    {
        _dbContext = dbContext;
        _locationService = locationService;
        _rankingService = rankingService;
        _intentService = intentService;
    }

    public async Task<DiscoveryResultResponse> DiscoverProvidersAsync(DiscoveryQuery query, CancellationToken cancellationToken = default)
    {
        // 1. Strict Input Validation (Reject illegal lat/lon, negative radius, broken bounding boxes)
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Parse Search Intent from Query Text
        SearchIntentResult? intent = null;
        if (!string.IsNullOrWhiteSpace(query.QueryText))
        {
            intent = _intentService.ParseIntent(query.QueryText);
        }

        // 3. Resolve Location and Geographic Context
        var locationRequest = new LocationResolutionRequest(
            PlaceId: query.SelectedPlace?.PlaceId,
            City: query.SelectedPlace?.City ?? intent?.ExtractedLocation,
            Area: query.SelectedPlace?.Area ?? intent?.ExtractedArea,
            PostalCode: query.SelectedPlace?.PostalCode ?? intent?.ExtractedPostcode,
            Latitude: query.Latitude,
            Longitude: query.Longitude,
            RadiusMeters: query.RadiusMeters,
            ViewportBounds: query.ViewportBounds ?? query.SelectedPlace?.ViewportBounds,
            IsCurrentGpsLocation: false
        );

        var resolvedLocation = await _locationService.ResolveLocationAsync(locationRequest, cancellationToken);

        // Effective category and service
        var effectiveCategory = !string.IsNullOrWhiteSpace(query.CategorySlug) && !query.CategorySlug.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? query.CategorySlug
            : intent?.ExtractedCategory;

        var effectiveService = !string.IsNullOrWhiteSpace(query.ServiceSlug) && !query.ServiceSlug.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? query.ServiceSlug
            : intent?.ExtractedService;

        // 4. Query Public, Active, Approved Businesses
        // Enforcement of server-side public visibility invariants
        var tenantsQuery = _dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => t.IsActive && t.IsPublished && t.ModerationStatus == ModerationStatus.Approved);

        // Filter by Rating at database level
        if (query.MinimumRating.HasValue && query.MinimumRating.Value > 0)
        {
            tenantsQuery = tenantsQuery.Where(t => t.AverageRating >= query.MinimumRating.Value);
        }

        // Filter by Category at database level
        if (!string.IsNullOrWhiteSpace(effectiveCategory) && !effectiveCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var catPrefix = effectiveCategory.Split('&')[0].Trim();
            tenantsQuery = tenantsQuery.Where(t =>
                t.Category.Contains(catPrefix) ||
                t.BusinessType.Contains(catPrefix));
        }

        var tenants = await tenantsQuery.ToListAsync(cancellationToken);
        if (tenants.Count == 0)
        {
            return new DiscoveryResultResponse(
                Items: new(),
                Markers: query.IncludeMarkers ? new() : null,
                TotalCount: 0,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalPages: 0,
                ResolvedLocation: resolvedLocation,
                SortModeUsed: query.SortMode.ToString()
            );
        }

        var tenantIds = tenants.Select(t => t.Id).ToList();

        // Load explicitly registered branches for these tenants
        var explicitLocations = await _dbContext.Locations.IgnoreQueryFilters()
            .Where(l => tenantIds.Contains(l.TenantId) && l.IsActive && !l.IsArchived)
            .ToListAsync(cancellationToken);

        // Group locations by tenant
        var locationsByTenant = explicitLocations
            .GroupBy(l => l.TenantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Load services and products for matching tenants
        var services = await _dbContext.Services.IgnoreQueryFilters()
            .Where(s => tenantIds.Contains(s.TenantId) && s.IsActive && !s.IsArchived)
            .ToListAsync(cancellationToken);

        var servicesByTenant = services
            .GroupBy(s => s.TenantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5. Expand Provider Location Candidates (A single business with multiple branches produces separate location results)
        var candidateLocations = new List<(Tenant Tenant, Guid LocationId, string Name, string Address, string City, string? Area, double Lat, double Lng)>();

        foreach (var tenant in tenants)
        {
            if (locationsByTenant.TryGetValue(tenant.Id, out var branches) && branches.Count > 0)
            {
                foreach (var branch in branches)
                {
                    candidateLocations.Add((
                        Tenant: tenant,
                        LocationId: branch.Id,
                        Name: string.IsNullOrWhiteSpace(branch.Name) ? tenant.Name : $"{tenant.Name} ({branch.Name})",
                        Address: branch.Address,
                        City: branch.City,
                        Area: ExtractAreaFromAddress(branch.Address),
                        Lat: branch.Latitude,
                        Lng: branch.Longitude
                    ));
                }
            }
            else
            {
                // Primary business location
                candidateLocations.Add((
                    Tenant: tenant,
                    LocationId: tenant.Id,
                    Name: tenant.Name,
                    Address: tenant.Address,
                    City: tenant.City,
                    Area: ExtractAreaFromAddress(tenant.Address),
                    Lat: tenant.Latitude,
                    Lng: tenant.Longitude
                ));
            }
        }

        // 6. Geographic Area Filtering
        // Priority: EffectiveViewportBounds > EffectiveRadiusMeters around reference coordinates > City/Area text filter
        var spatiallyFiltered = new List<(Tenant Tenant, Guid LocationId, string Name, string Address, string City, string? Area, double Lat, double Lng, double? DistanceMeters)>();

        var refCoords = resolvedLocation.ReferenceCoordinates;

        foreach (var candidate in candidateLocations)
        {
            // Viewport bounding box check
            if (resolvedLocation.EffectiveViewportBounds != null)
            {
                if (!resolvedLocation.EffectiveViewportBounds.Contains(candidate.Lat, candidate.Lng))
                {
                    continue;
                }
            }

            // Distance calculation if reference coordinates exist (NEVER fabricate distance)
            double? distanceMeters = null;
            if (refCoords != null)
            {
                distanceMeters = CalculateDistanceMeters(
                    refCoords.Latitude,
                    refCoords.Longitude,
                    candidate.Lat,
                    candidate.Lng
                );

                // Radius filtering
                if (resolvedLocation.EffectiveRadiusMeters.HasValue &&
                    distanceMeters.Value > resolvedLocation.EffectiveRadiusMeters.Value)
                {
                    continue;
                }
            }

            // City textual match if resolved as text fallback
            if (resolvedLocation.ResolutionType == LocationResolutionType.CityAreaTextFallback)
            {
                if (!string.IsNullOrWhiteSpace(resolvedLocation.FilterCity))
                {
                    bool cityMatch = candidate.City.Contains(resolvedLocation.FilterCity, StringComparison.OrdinalIgnoreCase) ||
                                     candidate.Address.Contains(resolvedLocation.FilterCity, StringComparison.OrdinalIgnoreCase);
                    if (!cityMatch) continue;
                }

                if (!string.IsNullOrWhiteSpace(resolvedLocation.FilterArea))
                {
                    bool areaMatch = candidate.Address.Contains(resolvedLocation.FilterArea, StringComparison.OrdinalIgnoreCase) ||
                                     candidate.Name.Contains(resolvedLocation.FilterArea, StringComparison.OrdinalIgnoreCase);
                    if (!areaMatch) continue;
                }
            }

            spatiallyFiltered.Add((candidate.Tenant, candidate.LocationId, candidate.Name, candidate.Address, candidate.City, candidate.Area, candidate.Lat, candidate.Lng, distanceMeters));
        }

        // 7. Business, Service, Price & Text Query Evaluation
        var eligibleResults = new List<ProviderLocationResult>();

        foreach (var item in spatiallyFiltered)
        {
            var tenantServices = servicesByTenant.TryGetValue(item.Tenant.Id, out var sList) ? sList : new();
            var serviceNames = tenantServices.Select(s => s.Name).ToList();

            decimal? startingPrice = tenantServices.Count > 0 ? tenantServices.Min(s => s.Price) : null;

            // Price filter
            if (query.MaximumPrice.HasValue)
            {
                if (startingPrice.HasValue && startingPrice.Value > query.MaximumPrice.Value)
                {
                    continue;
                }
            }

            // Service Slug filter
            if (!string.IsNullOrWhiteSpace(effectiveService))
            {
                bool hasService = tenantServices.Any(s =>
                    s.Name.Contains(effectiveService, StringComparison.OrdinalIgnoreCase));
                if (!hasService)
                {
                    continue;
                }
            }

            // Text Query Filter
            if (!string.IsNullOrWhiteSpace(query.QueryText))
            {
                var q = query.QueryText.Trim();
                bool matchesName = item.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
                bool matchesDesc = item.Tenant.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
                bool matchesCat = item.Tenant.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                  item.Tenant.BusinessType.Contains(q, StringComparison.OrdinalIgnoreCase);
                bool matchesService = tenantServices.Any(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
                bool matchesAddress = item.Address.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                      item.City.Contains(q, StringComparison.OrdinalIgnoreCase);

                // Intent clean query check
                bool matchesClean = !string.IsNullOrWhiteSpace(intent?.CleanQuery) &&
                                    (item.Name.Contains(intent.CleanQuery, StringComparison.OrdinalIgnoreCase) ||
                                     tenantServices.Any(s => s.Name.Contains(intent.CleanQuery, StringComparison.OrdinalIgnoreCase)));

                if (!matchesName && !matchesDesc && !matchesCat && !matchesService && !matchesAddress && !matchesClean)
                {
                    continue;
                }
            }

            // Prepare ranking context
            var rankingContext = new RankingContext(
                QueryText: query.QueryText,
                RequestedService: effectiveService,
                RequestedCategory: effectiveCategory,
                RequestedArea: resolvedLocation.FilterArea,
                RequestedCity: resolvedLocation.FilterCity,
                CandidateName: item.Name,
                CandidateCategory: item.Tenant.Category,
                CandidateBusinessType: item.Tenant.BusinessType,
                CandidateDescription: item.Tenant.Description,
                CandidateCity: item.City,
                CandidateAddress: item.Address,
                CandidateServices: serviceNames,
                DistanceMeters: item.DistanceMeters,
                AverageRating: item.Tenant.ReviewCount > 0 ? item.Tenant.AverageRating : null,
                ReviewCount: item.Tenant.ReviewCount,
                IsVerified: item.Tenant.VerificationStatus == VerificationStatus.Verified,
                IsPublished: item.Tenant.IsPublished,
                HasLogo: !string.IsNullOrWhiteSpace(item.Tenant.LogoUrl),
                HasCoverImage: !string.IsNullOrWhiteSpace(item.Tenant.CoverImageUrl),
                HasDetailedDescription: !string.IsNullOrWhiteSpace(item.Tenant.Description) && item.Tenant.Description.Length > 20,
                HasFullContactInfo: !string.IsNullOrWhiteSpace(item.Tenant.Address) && (!string.IsNullOrWhiteSpace(item.Tenant.Website) || !string.IsNullOrWhiteSpace(item.Tenant.Phone)),
                NextAvailableAt: null // Availability is on-demand via availability engine; never fabricated
            );

            var rankingScore = _rankingService.Evaluate(rankingContext);

            eligibleResults.Add(new ProviderLocationResult(
                BusinessId: item.Tenant.Id,
                BusinessSlug: item.Tenant.Slug,
                LocationId: item.LocationId,
                PublicName: item.Name,
                Category: item.Tenant.Category,
                PublicImage: item.Tenant.CoverImageUrl ?? item.Tenant.LogoUrl,
                PublicAddress: item.Address,
                City: item.City,
                Area: item.Area,
                Latitude: item.Lat,
                Longitude: item.Lng,
                DistanceMeters: item.DistanceMeters,
                RatingAverage: item.Tenant.ReviewCount > 0 ? item.Tenant.AverageRating : null,
                ReviewCount: item.Tenant.ReviewCount > 0 ? item.Tenant.ReviewCount : null,
                StartingPrice: startingPrice,
                Currency: item.Tenant.Currency ?? "USD",
                PublicServices: serviceNames.Take(4).ToList(),
                VerificationStatus: item.Tenant.VerificationStatus,
                NextAvailableAt: null,
                RankingScore: rankingScore.TotalWeightedScore
            ));
        }

        // 8. Sorting with Deterministic Secondary Tie-Breakers
        var sorted = ApplySorting(eligibleResults, query.SortMode);

        // 9. Server-side Pagination
        int totalCount = sorted.Count;
        int pageSize = Math.Clamp(query.PageSize, 1, DiscoveryQueryValidator.MaxAllowedPageSize);
        int page = Math.Max(1, query.Page);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // 10. Markers (if requested)
        List<DiscoveryMarkerDto>? markers = null;
        if (query.IncludeMarkers)
        {
            markers = sorted.Select(r => new DiscoveryMarkerDto(
                LocationId: r.LocationId,
                BusinessId: r.BusinessId,
                BusinessSlug: r.BusinessSlug,
                PublicName: r.PublicName,
                Latitude: r.Latitude,
                Longitude: r.Longitude,
                Category: r.Category,
                RatingAverage: r.RatingAverage,
                StartingPrice: r.StartingPrice
            )).ToList();
        }

        return new DiscoveryResultResponse(
            Items: pagedItems,
            Markers: markers,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages,
            ResolvedLocation: resolvedLocation,
            SortModeUsed: query.SortMode.ToString()
        );
    }

    private List<ProviderLocationResult> ApplySorting(List<ProviderLocationResult> items, DiscoverySortMode sortMode)
    {
        return sortMode switch
        {
            DiscoverySortMode.Nearest => items
                .OrderBy(i => i.DistanceMeters.HasValue ? 0 : 1)
                .ThenBy(i => i.DistanceMeters ?? double.MaxValue)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList(),

            DiscoverySortMode.TopRated => items
                .OrderByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenByDescending(i => i.ReviewCount ?? 0)
                .ThenBy(i => i.LocationId)
                .ToList(),

            DiscoverySortMode.MostReviewed => items
                .OrderByDescending(i => i.ReviewCount ?? 0)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList(),

            DiscoverySortMode.EarliestAvailable => items
                .OrderBy(i => i.NextAvailableAt.HasValue ? 0 : 1)
                .ThenBy(i => i.NextAvailableAt ?? DateTime.MaxValue)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList(),

            DiscoverySortMode.LowestPrice => items
                .OrderBy(i => i.StartingPrice.HasValue ? 0 : 1)
                .ThenBy(i => i.StartingPrice ?? decimal.MaxValue)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList(),

            DiscoverySortMode.HighestPrice => items
                .OrderBy(i => i.StartingPrice.HasValue ? 0 : 1)
                .ThenByDescending(i => i.StartingPrice ?? decimal.MinValue)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList(),

            _ => items // Recommended Sort
                .OrderByDescending(i => i.RankingScore ?? 0.0)
                .ThenByDescending(i => _rankingService.CalculateBayesianSmoothedRating(i.RatingAverage, i.ReviewCount ?? 0))
                .ThenBy(i => i.LocationId)
                .ToList()
        };
    }

    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(R * c, 1);
    }

    private static string? ExtractAreaFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        string[] knownAreas = { "Bodakdev", "Satellite", "Navrangpura", "Vastrapur", "Maninagar", "Sindhu Bhavan", "Bandra", "Andheri", "Juhu", "Indiranagar", "Koramangala", "Whitefield" };
        foreach (var a in knownAreas)
        {
            if (address.Contains(a, StringComparison.OrdinalIgnoreCase))
                return a;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[0] : null;
    }
}
