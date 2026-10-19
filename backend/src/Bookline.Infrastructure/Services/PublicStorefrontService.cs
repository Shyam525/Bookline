using System.Text.RegularExpressions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Infrastructure.Services;

public class PublicStorefrontService : IPublicStorefrontService
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly DayOfWeek[] DaysOfWeekOrder =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private readonly BooklineDbContext _dbContext;

    public PublicStorefrontService(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static bool IsValidSlug(string? slug, out string normalizedSlug)
    {
        normalizedSlug = string.Empty;
        if (string.IsNullOrWhiteSpace(slug)) return false;
        normalizedSlug = slug.Trim().ToLowerInvariant();
        return SlugRegex.IsMatch(normalizedSlug);
    }

    private async Task<Tenant?> GetEligibleTenantBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug, out var normalizedSlug)) return null;

        return await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == normalizedSlug
                && t.IsActive
                && t.IsPublished
                && t.VerificationStatus != VerificationStatus.Suspended
                && t.ModerationStatus != ModerationStatus.Rejected
                && t.ModerationStatus != ModerationStatus.Suspended,
                cancellationToken);
    }

    public async Task<PublicStorefrontResponse?> GetStorefrontBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        var locations = await _dbContext.Locations.IgnoreQueryFilters()
            .Where(l => l.TenantId == tenant.Id && l.IsActive && !l.IsArchived)
            .ToListAsync(cancellationToken);

        var categories = await _dbContext.ServiceCategories.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        var services = await _dbContext.Services.IgnoreQueryFilters()
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .Where(s => s.TenantId == tenant.Id && s.IsActive && !s.IsArchived && s.IsPublished)
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
            .Take(20)
            .ToListAsync(cancellationToken);

        var workingHours = await _dbContext.WorkingHours.IgnoreQueryFilters()
            .Where(wh => wh.TenantId == tenant.Id)
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
            s.IsOnlineBookingEnabled,
            s.Slug,
            s.IsPublished,
            s.ImageUrl,
            s.StaffServices.Select(ss => ss.StaffId).ToList(),
            s.ServiceLocations.Select(sl => sl.LocationId).ToList()
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
            p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
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
            r.CreatedAtUtc,
            r.Title,
            r.ProviderResponse
        )).ToList();

        var hoursByDay = workingHours
            .GroupBy(wh => wh.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => new PublicBusinessHoursDto(
                    g.Key.ToString(),
                    g.Min(wh => wh.StartTime).ToString("HH:mm"),
                    g.Max(wh => wh.EndTime).ToString("HH:mm"),
                    false
                )
            );

        var hoursDtos = DaysOfWeekOrder
            .Select(d => hoursByDay.TryGetValue(d, out var dto)
                ? dto
                : new PublicBusinessHoursDto(d.ToString(), "00:00", "00:00", true))
            .ToList();

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

        return new PublicStorefrontResponse(
            providerDto,
            locationDtos,
            categoryDtos,
            serviceDtos,
            productDtos,
            staffDtos,
            reviewDtos,
            hoursDtos
        );
    }

    public async Task<IReadOnlyList<PublicServiceDto>?> GetServicesBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        return await _dbContext.Services.IgnoreQueryFilters()
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .Where(s => s.TenantId == tenant.Id && s.IsActive && !s.IsArchived && s.IsPublished)
            .Select(s => new PublicServiceDto(
                s.Id,
                s.CategoryId,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.BufferMinutes,
                s.Price,
                tenant.Currency ?? "USD",
                s.IsOnlineBookingEnabled,
                s.Slug,
                s.IsPublished,
                s.ImageUrl,
                s.StaffServices.Select(ss => ss.StaffId).ToList(),
                s.ServiceLocations.Select(sl => sl.LocationId).ToList()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicServiceDto?> GetServiceBySlugAsync(string businessSlug, string serviceSlug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(businessSlug, cancellationToken);
        if (tenant == null) return null;

        if (string.IsNullOrWhiteSpace(serviceSlug)) return null;
        var normalizedServiceSlug = serviceSlug.Trim().ToLowerInvariant();

        var service = await _dbContext.Services.IgnoreQueryFilters()
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Slug == normalizedServiceSlug && s.IsActive && !s.IsArchived && s.IsPublished, cancellationToken);

        if (service == null) return null;

        return new PublicServiceDto(
            service.Id,
            service.CategoryId,
            service.Name,
            service.Description,
            service.DurationMinutes,
            service.BufferMinutes,
            service.Price,
            tenant.Currency ?? "USD",
            service.IsOnlineBookingEnabled,
            service.Slug,
            service.IsPublished,
            service.ImageUrl,
            service.StaffServices.Select(ss => ss.StaffId).ToList(),
            service.ServiceLocations.Select(sl => sl.LocationId).ToList()
        );
    }

    public async Task<IReadOnlyList<PublicProductDto>?> GetProductsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        return await _dbContext.Products.IgnoreQueryFilters()
            .Include(p => p.Images)
            .Where(p => p.TenantId == tenant.Id && p.IsActive && p.IsPublished)
            .Select(p => new PublicProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                tenant.Currency ?? "USD",
                p.StockQuantity,
                p.Sku,
                p.ImageUrl,
                !p.TrackInventory || p.StockQuantity - p.ReservedQuantity > 0,
                p.Slug,
                p.IsPurchasableOnline,
                p.TrackInventory ? Math.Max(0, p.StockQuantity - p.ReservedQuantity) : int.MaxValue,
                p.CategoryId,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicLocationDto>?> GetLocationsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        return await _dbContext.Locations.IgnoreQueryFilters()
            .Where(l => l.TenantId == tenant.Id && l.IsActive && !l.IsArchived)
            .Select(l => new PublicLocationDto(
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
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PublicReviewDto> Items, int TotalCount)?> GetReviewsBySlugAsync(string slug, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        var query = _dbContext.Reviews.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenant.Id && r.ModerationStatus == ModerationStatus.Approved)
            .OrderByDescending(r => r.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);

        var reviews = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(r => new PublicReviewDto(
                r.Id,
                r.CustomerName,
                r.Rating,
                r.Comment,
                r.CreatedAtUtc,
                r.Title,
                r.ProviderResponse
            ))
            .ToListAsync(cancellationToken);

        return (reviews, total);
    }

    public async Task<IReadOnlyList<PublicBusinessHoursDto>?> GetHoursBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEligibleTenantBySlugAsync(slug, cancellationToken);
        if (tenant == null) return null;

        var workingHours = await _dbContext.WorkingHours.IgnoreQueryFilters()
            .Where(wh => wh.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        var hoursByDay = workingHours
            .GroupBy(wh => wh.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => new PublicBusinessHoursDto(
                    g.Key.ToString(),
                    g.Min(wh => wh.StartTime).ToString("HH:mm"),
                    g.Max(wh => wh.EndTime).ToString("HH:mm"),
                    false
                )
            );

        return DaysOfWeekOrder
            .Select(d => hoursByDay.TryGetValue(d, out var dto)
                ? dto
                : new PublicBusinessHoursDto(d.ToString(), "00:00", "00:00", true))
            .ToList();
    }
}
