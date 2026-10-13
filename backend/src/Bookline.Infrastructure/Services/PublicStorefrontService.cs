using Bookline.Application.Common.Interfaces;
using Bookline.Application.Discovery.Dtos;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Infrastructure.Services;

public class PublicStorefrontService : IPublicStorefrontService
{
    private readonly BooklineDbContext _dbContext;

    public PublicStorefrontService(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublicStorefrontResponse?> GetStorefrontBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == normalizedSlug && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

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

        var hoursDtos = workingHours
            .GroupBy(wh => wh.DayOfWeek)
            .Select(g =>
            {
                var first = g.First();
                return new PublicBusinessHoursDto(
                    g.Key.ToString(),
                    first.StartTime.ToString("HH:mm"),
                    first.EndTime.ToString("HH:mm"),
                    false
                );
            })
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
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

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
        if (string.IsNullOrWhiteSpace(businessSlug) || string.IsNullOrWhiteSpace(serviceSlug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == businessSlug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

        if (tenant == null) return null;

        var service = await _dbContext.Services.IgnoreQueryFilters()
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Slug == serviceSlug.ToLowerInvariant() && s.IsActive && !s.IsArchived && s.IsPublished, cancellationToken);

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
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

        if (tenant == null) return null;

        return await _dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenant.Id && p.IsActive && p.IsPurchasableOnline)
            .Select(p => new PublicProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                tenant.Currency ?? "USD",
                p.StockQuantity,
                p.Sku,
                p.ImageUrl,
                p.StockQuantity > 0
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicLocationDto>?> GetLocationsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

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
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

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
                r.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return (reviews, total);
    }

    public async Task<IReadOnlyList<PublicBusinessHoursDto>?> GetHoursBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive && t.IsPublished && t.VerificationStatus != VerificationStatus.Suspended, cancellationToken);

        if (tenant == null) return null;

        var workingHours = await _dbContext.WorkingHours.IgnoreQueryFilters()
            .Where(wh => wh.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        return workingHours
            .GroupBy(wh => wh.DayOfWeek)
            .Select(g =>
            {
                var first = g.First();
                return new PublicBusinessHoursDto(
                    g.Key.ToString(),
                    first.StartTime.ToString("HH:mm"),
                    first.EndTime.ToString("HH:mm"),
                    false
                );
            })
            .ToList();
    }
}
