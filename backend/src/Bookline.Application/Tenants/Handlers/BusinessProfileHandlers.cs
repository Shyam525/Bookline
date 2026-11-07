using System.Text.Json;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Tenants.Handlers;

public class BusinessProfileHandlers :
    IRequestHandler<GetBusinessProfileQuery, BusinessProfileDto>,
    IRequestHandler<UpdateBusinessProfileCommand, BusinessProfileDto>,
    IRequestHandler<GetBusinessPoliciesQuery, BusinessPoliciesDto>,
    IRequestHandler<UpdateBusinessPoliciesCommand, BusinessPoliciesDto>,
    IRequestHandler<CheckSlugAvailabilityQuery, CheckSlugAvailabilityResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public BusinessProfileHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<BusinessProfileDto> Handle(GetBusinessProfileQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        var primaryLocation = await _context.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived && l.IsPrimary, cancellationToken)
            ?? await _context.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived, cancellationToken);

        return MapToProfileDto(tenant, primaryLocation);
    }

    public async Task<BusinessProfileDto> Handle(UpdateBusinessProfileCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        var targetSlug = req.Slug.Trim().ToLowerInvariant();
        var slugExists = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Id != tenant.Id && t.Slug == targetSlug, cancellationToken);

        if (slugExists)
        {
            throw new ValidationException("SLUG_ALREADY_EXISTS: The specified public slug is already registered to another business.");
        }

        tenant.Name = req.Name.Trim();
        tenant.Slug = targetSlug;
        tenant.Description = req.Description.Trim();
        tenant.Category = req.Category.Trim();
        tenant.BusinessType = req.BusinessType.Trim();
        tenant.Phone = req.Phone?.Trim();
        tenant.Email = req.Email?.Trim();
        tenant.Website = req.Website?.Trim();
        tenant.Address = req.Address.Trim();
        tenant.City = req.City.Trim();
        tenant.Area = req.Area.Trim();
        tenant.State = req.State.Trim();
        tenant.Country = req.Country.Trim();
        tenant.PostalCode = req.PostalCode.Trim();
        if (req.Latitude.HasValue) tenant.Latitude = req.Latitude.Value;
        if (req.Longitude.HasValue) tenant.Longitude = req.Longitude.Value;
        tenant.TimeZoneId = req.TimeZoneId.Trim();
        tenant.LogoUrl = req.LogoUrl;
        tenant.CoverImageUrl = req.CoverImageUrl;
        if (req.IsPublished.HasValue) tenant.IsPublished = req.IsPublished.Value;

        if (req.GalleryImages != null)
        {
            tenant.GalleryImagesJson = JsonSerializer.Serialize(req.GalleryImages);
        }

        // Synchronize primary Location for single-location MVP
        var location = await _context.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived && l.IsPrimary, cancellationToken)
            ?? await _context.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived, cancellationToken);

        if (location == null)
        {
            location = new Location
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = $"{tenant.Name} Flagship",
                Address = tenant.Address,
                City = tenant.City,
                Area = tenant.Area,
                State = tenant.State,
                Country = tenant.Country,
                PostalCode = tenant.PostalCode,
                Latitude = tenant.Latitude,
                Longitude = tenant.Longitude,
                Phone = tenant.Phone ?? string.Empty,
                Timezone = tenant.TimeZoneId,
                Currency = tenant.Currency,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Locations.Add(location);
        }
        else
        {
            location.Name = $"{tenant.Name} Flagship";
            location.Address = tenant.Address;
            location.City = tenant.City;
            location.Area = tenant.Area;
            location.State = tenant.State;
            location.Country = tenant.Country;
            location.PostalCode = tenant.PostalCode;
            location.Latitude = tenant.Latitude;
            location.Longitude = tenant.Longitude;
            if (!string.IsNullOrWhiteSpace(tenant.Phone)) location.Phone = tenant.Phone;
            location.Timezone = tenant.TimeZoneId;
            location.Currency = tenant.Currency;
            location.IsPrimary = true;
            location.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapToProfileDto(tenant, location);
    }

    public async Task<BusinessPoliciesDto> Handle(GetBusinessPoliciesQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        var weeklyHours = ParseBusinessHours(tenant.BusinessHoursJson);

        return new BusinessPoliciesDto(
            tenant.HoldDurationMinutes,
            tenant.MinimumNoticeHours,
            tenant.BookingHorizonDays,
            tenant.CancellationWindowHours,
            tenant.CancellationPolicy,
            tenant.ReschedulingWindowHours,
            tenant.ReschedulingPolicy,
            weeklyHours
        );
    }

    public async Task<BusinessPoliciesDto> Handle(UpdateBusinessPoliciesCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        tenant.HoldDurationMinutes = req.HoldDurationMinutes;
        tenant.MinimumNoticeHours = req.MinimumNoticeHours;
        tenant.BookingHorizonDays = req.BookingHorizonDays;
        tenant.CancellationWindowHours = req.CancellationWindowHours;
        tenant.CancellationPolicy = req.CancellationPolicy;
        tenant.ReschedulingWindowHours = req.ReschedulingWindowHours;
        tenant.ReschedulingPolicy = req.ReschedulingPolicy;

        if (req.WeeklyHours != null)
        {
            tenant.BusinessHoursJson = JsonSerializer.Serialize(req.WeeklyHours);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new BusinessPoliciesDto(
            tenant.HoldDurationMinutes,
            tenant.MinimumNoticeHours,
            tenant.BookingHorizonDays,
            tenant.CancellationWindowHours,
            tenant.CancellationPolicy,
            tenant.ReschedulingWindowHours,
            tenant.ReschedulingPolicy,
            ParseBusinessHours(tenant.BusinessHoursJson)
        );
    }

    public async Task<CheckSlugAvailabilityResponse> Handle(CheckSlugAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return new CheckSlugAvailabilityResponse(false, "", "Slug cannot be empty.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var exists = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Id != _tenantContext.TenantId && t.Slug == normalizedSlug, cancellationToken);

        return new CheckSlugAvailabilityResponse(!exists, normalizedSlug, exists ? "Slug is already taken." : "Slug is available.");
    }

    private static BusinessProfileDto MapToProfileDto(Tenant tenant, Location? primaryLocation)
    {
        var gallery = new List<string>();
        if (!string.IsNullOrWhiteSpace(tenant.GalleryImagesJson))
        {
            try
            {
                gallery = JsonSerializer.Deserialize<List<string>>(tenant.GalleryImagesJson) ?? new List<string>();
            }
            catch
            {
                gallery = new List<string>();
            }
        }

        return new BusinessProfileDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Description,
            tenant.Category,
            tenant.BusinessType,
            tenant.Phone,
            tenant.Email,
            tenant.Website,
            tenant.Address,
            tenant.City,
            tenant.Area,
            tenant.State,
            tenant.Country,
            tenant.PostalCode,
            tenant.Latitude,
            tenant.Longitude,
            tenant.TimeZoneId,
            tenant.Currency,
            tenant.IsPublished,
            tenant.IsActive,
            tenant.LogoUrl,
            tenant.CoverImageUrl,
            gallery,
            primaryLocation?.Id,
            primaryLocation?.Name
        );
    }

    private static List<BusinessDayHours> ParseBusinessHours(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<BusinessDayHours>>(json);
                if (parsed != null && parsed.Any()) return parsed;
            }
            catch
            {
                // Fallback to defaults
            }
        }

        // Default: Mon-Sat 09:00 - 20:00 (with lunch break 13:00 - 14:00), Sun Closed
        var defaults = new List<BusinessDayHours>();
        for (int i = 0; i <= 6; i++)
        {
            var day = (DayOfWeek)i;
            var isSunday = day == DayOfWeek.Sunday;
            defaults.Add(new BusinessDayHours(
                day,
                !isSunday,
                isSunday ? null : new TimeOnly(9, 0),
                isSunday ? null : new TimeOnly(20, 0),
                isSunday ? null : new List<BusinessBreakPeriod>
                {
                    new(new TimeOnly(13, 0), new TimeOnly(14, 0), "Lunch Break")
                }
            ));
        }
        return defaults;
    }
}

