using System.Text.RegularExpressions;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.Tenants.Handlers;

public class OnboardingHandlers :
    IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>,
    IRequestHandler<SaveOnboardingStepCommand, OnboardingStatusDto>,
    IRequestHandler<GetOnboardingReadinessQuery, OnboardingReadinessDto>,
    IRequestHandler<PublishStorefrontCommand, PublishStorefrontResultDto>,
    IRequestHandler<CompleteOnboardingCommand, OnboardingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public OnboardingHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        return await MapToDtoAsync(tenant, cancellationToken);
    }

    public async Task<OnboardingStatusDto> Handle(SaveOnboardingStepCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        // Step 1: Account & Membership
        if (req.Step == 1 || !string.IsNullOrWhiteSpace(req.OwnerName) || !string.IsNullOrWhiteSpace(req.OwnerPhone))
        {
            var ownerUser = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && (u.Role == "Owner" || u.Role == "Provider"), cancellationToken);

            if (ownerUser != null)
            {
                if (!string.IsNullOrWhiteSpace(req.OwnerName))
                {
                    var parts = req.OwnerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    ownerUser.FirstName = parts[0];
                    ownerUser.LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
                if (!string.IsNullOrWhiteSpace(req.OwnerPhone))
                {
                    ownerUser.Phone = req.OwnerPhone.Trim();
                }
                ownerUser.UpdatedAtUtc = DateTime.UtcNow;

                var membership = await _context.OrganizationMemberships.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m => m.TenantId == tenant.Id && m.UserId == ownerUser.Id, cancellationToken);

                if (membership == null)
                {
                    _context.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        UserId = ownerUser.Id,
                        Role = "Owner",
                        IsActive = true,
                        JoinedAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    membership.Role = "Owner";
                    membership.IsActive = true;
                }
            }
        }

        // Step 2: Business Creation
        if (req.Step == 2 || !string.IsNullOrWhiteSpace(req.BusinessName))
        {
            if (req.Step == 2 && string.IsNullOrWhiteSpace(req.BusinessName))
            {
                throw new ValidationException("Business name is required.");
            }
            if (!string.IsNullOrWhiteSpace(req.BusinessName))
            {
                tenant.Name = req.BusinessName.Trim();
                tenant.Slug = !string.IsNullOrWhiteSpace(req.BusinessSlug) 
                    ? req.BusinessSlug.Trim() 
                    : Slugify(req.BusinessName);
            }
        }

        // Step 3: Business Type & Category
        if (req.Step == 3 || !string.IsNullOrWhiteSpace(req.BusinessType) || !string.IsNullOrWhiteSpace(req.Category))
        {
            if (!string.IsNullOrWhiteSpace(req.BusinessType)) tenant.BusinessType = req.BusinessType.Trim();
            if (!string.IsNullOrWhiteSpace(req.Category)) tenant.Category = req.Category.Trim();
        }

        // Step 4: Business Profile
        if (req.Step == 4 || !string.IsNullOrWhiteSpace(req.Description) || !string.IsNullOrWhiteSpace(req.Website) || !string.IsNullOrWhiteSpace(req.Phone))
        {
            if (!string.IsNullOrWhiteSpace(req.Description)) tenant.Description = req.Description.Trim();
            if (!string.IsNullOrWhiteSpace(req.Website)) tenant.Website = req.Website.Trim();
            if (!string.IsNullOrWhiteSpace(req.Phone)) tenant.Phone = req.Phone.Trim();
        }

        // Step 5: First Location
        if (req.Step == 5 || !string.IsNullOrWhiteSpace(req.Address))
        {
            if (!string.IsNullOrWhiteSpace(req.Address))
            {
                tenant.Address = req.Address.Trim();
                if (!string.IsNullOrWhiteSpace(req.City)) tenant.City = req.City.Trim();
                if (!string.IsNullOrWhiteSpace(req.State)) tenant.State = req.State.Trim();
                if (!string.IsNullOrWhiteSpace(req.PostalCode)) tenant.PostalCode = req.PostalCode.Trim();
                if (!string.IsNullOrWhiteSpace(req.Country)) tenant.Country = req.Country.Trim();

                var location = await _context.Locations.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived, cancellationToken);

                if (location == null)
                {
                    location = new Location
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Name = req.LocationName?.Trim() ?? $"{tenant.Name} Flagship",
                        Address = req.Address.Trim(),
                        City = req.City?.Trim() ?? tenant.City,
                        State = req.State?.Trim() ?? tenant.State,
                        Country = req.Country?.Trim() ?? tenant.Country,
                        PostalCode = req.PostalCode?.Trim() ?? tenant.PostalCode,
                        Timezone = tenant.TimeZoneId,
                        Currency = tenant.Currency,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _context.Locations.Add(location);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(req.LocationName)) location.Name = req.LocationName.Trim();
                    location.Address = req.Address.Trim();
                    if (!string.IsNullOrWhiteSpace(req.City)) location.City = req.City.Trim();
                    if (!string.IsNullOrWhiteSpace(req.State)) location.State = req.State.Trim();
                    if (!string.IsNullOrWhiteSpace(req.PostalCode)) location.PostalCode = req.PostalCode.Trim();
                    if (!string.IsNullOrWhiteSpace(req.Country)) location.Country = req.Country.Trim();
                    location.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        // Step 6: Timezone and Currency
        if (req.Step == 6 || !string.IsNullOrWhiteSpace(req.TimeZoneId) || !string.IsNullOrWhiteSpace(req.Currency))
        {
            if (!string.IsNullOrWhiteSpace(req.TimeZoneId))
            {
                var tz = NodaTime.DateTimeZoneProviders.Tzdb.GetZoneOrNull(req.TimeZoneId.Trim());
                if (tz == null)
                {
                    throw new ValidationException("Invalid IANA timezone identifier.");
                }
                tenant.TimeZoneId = req.TimeZoneId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Currency))
            {
                tenant.Currency = req.Currency.Trim().ToUpperInvariant();
            }

            // Sync location timezone/currency without mutating existing appointments (Section 28 Step 6)
            var loc = await _context.Locations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived, cancellationToken);
            if (loc != null)
            {
                loc.Timezone = tenant.TimeZoneId;
                loc.Currency = tenant.Currency;
                loc.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        // Step 7: Add Services
        if (req.Step == 7 || !string.IsNullOrWhiteSpace(req.FirstServiceName))
        {
            if (req.Step == 7 && string.IsNullOrWhiteSpace(req.FirstServiceName))
            {
                throw new ValidationException("Service name is required.");
            }
            if (req.FirstServiceDurationMinutes.HasValue && req.FirstServiceDurationMinutes.Value <= 0)
            {
                throw new ValidationException("Service duration must be greater than zero.");
            }
            if (req.FirstServicePrice.HasValue && req.FirstServicePrice.Value < 0)
            {
                throw new ValidationException("Service price cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(req.FirstServiceName))
            {
                var service = await _context.Services.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

                if (service == null)
                {
                    service = new Service
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Name = req.FirstServiceName.Trim(),
                        Slug = Slugify(req.FirstServiceName),
                        DurationMinutes = req.FirstServiceDurationMinutes ?? 45,
                        Price = req.FirstServicePrice ?? 50.00m,
                        Currency = tenant.Currency,
                        BufferMinutes = 15,
                        IsActive = true,
                        IsPublished = true,
                        IsOnlineBookingEnabled = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _context.Services.Add(service);
                }
                else
                {
                    service.Name = req.FirstServiceName.Trim();
                    service.Slug = Slugify(req.FirstServiceName);
                    if (req.FirstServiceDurationMinutes.HasValue) service.DurationMinutes = req.FirstServiceDurationMinutes.Value;
                    if (req.FirstServicePrice.HasValue) service.Price = req.FirstServicePrice.Value;
                    service.Currency = tenant.Currency;
                    service.IsActive = true;
                    service.IsPublished = true;
                    service.IsOnlineBookingEnabled = true;
                    service.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        // Step 8: Add Products (Optional retail merchandise)
        if (req.Step == 8 || !string.IsNullOrWhiteSpace(req.FirstProductName))
        {
            if (req.FirstProductPrice.HasValue && req.FirstProductPrice.Value < 0)
            {
                throw new ValidationException("Product price cannot be negative.");
            }
            if (req.FirstProductStock.HasValue && req.FirstProductStock.Value < 0)
            {
                throw new ValidationException("Product stock quantity cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(req.FirstProductName))
            {
                var product = await _context.Products.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.IsActive, cancellationToken);

                if (product == null)
                {
                    product = new Product
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Name = req.FirstProductName.Trim(),
                        Slug = Slugify(req.FirstProductName),
                        Price = req.FirstProductPrice ?? 25.00m,
                        Currency = tenant.Currency,
                        Sku = req.FirstProductSku?.Trim() ?? $"SKU-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                        StockQuantity = req.FirstProductStock ?? 10,
                        TrackInventory = true,
                        IsActive = true,
                        IsPurchasableOnline = true,
                        IsPublished = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _context.Products.Add(product);
                }
                else
                {
                    product.Name = req.FirstProductName.Trim();
                    product.Slug = Slugify(req.FirstProductName);
                    if (req.FirstProductPrice.HasValue) product.Price = req.FirstProductPrice.Value;
                    if (!string.IsNullOrWhiteSpace(req.FirstProductSku)) product.Sku = req.FirstProductSku.Trim();
                    if (req.FirstProductStock.HasValue) product.StockQuantity = req.FirstProductStock.Value;
                    product.Currency = tenant.Currency;
                    product.IsActive = true;
                    product.IsPurchasableOnline = true;
                    product.IsPublished = true;
                    product.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        // Step 9: Add Staff
        if (req.Step == 9 || !string.IsNullOrWhiteSpace(req.FirstStaffName))
        {
            if (req.Step == 9 && string.IsNullOrWhiteSpace(req.FirstStaffName))
            {
                throw new ValidationException("Staff name is required.");
            }

            if (!string.IsNullOrWhiteSpace(req.FirstStaffName))
            {
                var staff = await _context.Staff.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

                if (staff == null)
                {
                    staff = new DomainStaff
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Name = req.FirstStaffName.Trim(),
                        Title = req.FirstStaffTitle?.Trim() ?? "Specialist",
                        TimeZoneId = tenant.TimeZoneId,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _context.Staff.Add(staff);
                }
                else
                {
                    staff.Name = req.FirstStaffName.Trim();
                    if (!string.IsNullOrWhiteSpace(req.FirstStaffTitle)) staff.Title = req.FirstStaffTitle.Trim();
                    staff.TimeZoneId = tenant.TimeZoneId;
                    staff.IsActive = true;
                    staff.UpdatedAtUtc = DateTime.UtcNow;
                }

                // Link staff to primary service if available
                var service = await _context.Services.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

                if (service != null)
                {
                    var hasLink = await _context.StaffServices.IgnoreQueryFilters()
                        .AnyAsync(ss => ss.StaffId == staff.Id && ss.ServiceId == service.Id, cancellationToken);

                    if (!hasLink)
                    {
                        _context.StaffServices.Add(new StaffService
                        {
                            TenantId = tenant.Id,
                            StaffId = staff.Id,
                            ServiceId = service.Id
                        });
                    }
                }
            }
        }

        // Step 10: Configure Working Hours
        if (req.Step == 10 || (req.StartTime.HasValue && req.EndTime.HasValue))
        {
            if (req.StartTime.HasValue && req.EndTime.HasValue && req.EndTime.Value <= req.StartTime.Value)
            {
                throw new ValidationException("Operating closing time must be after opening time.");
            }

            var staff = await _context.Staff.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

            if (staff != null && req.StartTime.HasValue && req.EndTime.HasValue)
            {
                var existingHours = await _context.WorkingHours.IgnoreQueryFilters()
                    .Where(w => w.StaffId == staff.Id)
                    .ToListAsync(cancellationToken);

                if (existingHours.Any())
                {
                    _context.WorkingHours.RemoveRange(existingHours);
                }

                var days = req.WorkingDays != null && req.WorkingDays.Any()
                    ? req.WorkingDays
                    : new List<int> { 1, 2, 3, 4, 5 }; // Mon - Fri default

                foreach (var day in days)
                {
                    _context.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = tenant.Id,
                        StaffId = staff.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = req.StartTime.Value,
                        EndTime = req.EndTime.Value
                    });
                }
            }
        }

        // Step 11: Configure Booking Rules
        if (req.Step == 11 || req.HoldDurationMinutes.HasValue || req.MinimumNoticeHours.HasValue || req.BookingHorizonDays.HasValue)
        {
            if (req.HoldDurationMinutes.HasValue && req.HoldDurationMinutes.Value <= 0)
            {
                throw new ValidationException("Hold duration must be greater than zero.");
            }
            if (req.BookingHorizonDays.HasValue && req.BookingHorizonDays.Value <= 0)
            {
                throw new ValidationException("Booking horizon must be at least 1 day.");
            }
            if (req.MinimumNoticeHours.HasValue && req.MinimumNoticeHours.Value < 0)
            {
                throw new ValidationException("Minimum notice hours cannot be negative.");
            }

            if (req.HoldDurationMinutes.HasValue) tenant.HoldDurationMinutes = req.HoldDurationMinutes.Value;
            if (req.MinimumNoticeHours.HasValue) tenant.MinimumNoticeHours = req.MinimumNoticeHours.Value;
            if (req.BookingHorizonDays.HasValue) tenant.BookingHorizonDays = req.BookingHorizonDays.Value;
        }

        // Persist progress server-side based on actual steps
        tenant.OnboardingStep = Math.Max(tenant.OnboardingStep, req.Step + 1);

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(tenant, cancellationToken);
    }

    public async Task<OnboardingReadinessDto> Handle(GetOnboardingReadinessQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        var hasAccountMembership = tenant.Id != Guid.Empty;
        var hasBusinessInfo = !string.IsNullOrWhiteSpace(tenant.Name);
        var hasBusinessType = !string.IsNullOrWhiteSpace(tenant.BusinessType);
        var hasProfile = !string.IsNullOrWhiteSpace(tenant.Description);
        var hasLocation = await _context.Locations.IgnoreQueryFilters()
            .AnyAsync(l => l.TenantId == tenant.Id && l.IsActive && !l.IsArchived, cancellationToken);
        var hasTimezoneAndCurrency = !string.IsNullOrWhiteSpace(tenant.TimeZoneId) && !string.IsNullOrWhiteSpace(tenant.Currency);
        var hasServices = await _context.Services.IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == tenant.Id && s.IsActive && !s.IsArchived, cancellationToken);
        var hasProducts = await _context.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == tenant.Id && p.IsActive, cancellationToken);
        var hasStaff = await _context.Staff.IgnoreQueryFilters()
            .AnyAsync(st => st.TenantId == tenant.Id && st.IsActive && !st.IsArchived, cancellationToken);
        var hasWorkingHours = await _context.WorkingHours.IgnoreQueryFilters()
            .AnyAsync(w => w.TenantId == tenant.Id, cancellationToken);
        var hasBookingRules = tenant.HoldDurationMinutes > 0 && tenant.BookingHorizonDays > 0;

        var requirements = new List<OnboardingReadinessRequirement>
        {
            new OnboardingReadinessRequirement(
                "account_membership",
                "Account & Organization Membership",
                "Verified account credentials and active owner membership.",
                1,
                hasAccountMembership,
                true,
                "/provider/onboarding?step=1"
            ),
            new OnboardingReadinessRequirement(
                "business_name",
                "Business Identity & Name",
                "A registered business legal name and custom URL slug.",
                2,
                hasBusinessInfo,
                true,
                "/provider/onboarding?step=2"
            ),
            new OnboardingReadinessRequirement(
                "business_type",
                "Business Category & Classification",
                "Operational sector and primary industry classification.",
                3,
                hasBusinessType,
                true,
                "/provider/onboarding?step=3"
            ),
            new OnboardingReadinessRequirement(
                "business_profile",
                "Storefront Description & Profile",
                "Public overview explaining client experience and services.",
                4,
                hasProfile,
                true,
                "/provider/onboarding?step=4"
            ),
            new OnboardingReadinessRequirement(
                "primary_location",
                "Operational Physical Location",
                "At least one verified location with an address.",
                5,
                hasLocation,
                true,
                "/provider/onboarding?step=5"
            ),
            new OnboardingReadinessRequirement(
                "timezone_currency",
                "Timezone & Currency Policy",
                "Valid IANA timezone and standard currency code.",
                6,
                hasTimezoneAndCurrency,
                true,
                "/provider/onboarding?step=6"
            ),
            new OnboardingReadinessRequirement(
                "active_service",
                "Active Bookable Treatment / Service",
                "At least one published service enabled for online scheduling.",
                7,
                hasServices,
                true,
                "/provider/onboarding?step=7"
            ),
            new OnboardingReadinessRequirement(
                "retail_products",
                "Retail Product Catalog (Optional)",
                "Physical merchandise available for purchase during or without booking.",
                8,
                hasProducts,
                false, // Optional
                "/provider/onboarding?step=8"
            ),
            new OnboardingReadinessRequirement(
                "active_staff",
                "Specialist / Staff Member",
                "At least one active specialist available to service clients.",
                9,
                hasStaff,
                true,
                "/provider/onboarding?step=9"
            ),
            new OnboardingReadinessRequirement(
                "working_hours",
                "Weekly Operating Schedule",
                "Standard weekly operating hours configured for specialists.",
                10,
                hasWorkingHours,
                true,
                "/provider/onboarding?step=10"
            ),
            new OnboardingReadinessRequirement(
                "booking_rules",
                "Booking & Slot Hold Policies",
                "Enforced advance booking horizons and slot hold windows.",
                11,
                hasBookingRules,
                true,
                "/provider/onboarding?step=11"
            )
        };

        var totalMandatory = requirements.Count(r => r.IsMandatory);
        var satisfiedMandatory = requirements.Count(r => r.IsMandatory && r.IsSatisfied);
        var allMandatorySatisfied = totalMandatory == satisfiedMandatory;

        return new OnboardingReadinessDto(
            IsReady: allMandatorySatisfied,
            TotalMandatory: totalMandatory,
            SatisfiedMandatory: satisfiedMandatory,
            AllMandatorySatisfied: allMandatorySatisfied,
            Requirements: requirements
        );
    }

    public async Task<PublishStorefrontResultDto> Handle(PublishStorefrontCommand request, CancellationToken cancellationToken)
    {
        var readiness = await Handle(new GetOnboardingReadinessQuery(), cancellationToken);

        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        if (tenant.VerificationStatus == VerificationStatus.Suspended || tenant.ModerationStatus == ModerationStatus.Suspended)
        {
            return new PublishStorefrontResultDto(
                Success: false,
                IsPublished: false,
                StorefrontUrl: string.Empty,
                Message: "Storefront publication rejected. The organization is currently suspended by platform administration.",
                MissingRequirements: new List<string> { "Platform account suspension must be resolved before publication." }
            );
        }

        if (!tenant.IsActive || tenant.ModerationStatus == ModerationStatus.Rejected)
        {
            return new PublishStorefrontResultDto(
                Success: false,
                IsPublished: false,
                StorefrontUrl: string.Empty,
                Message: "Storefront publication rejected. The organization is inactive or rejected by platform administration.",
                MissingRequirements: new List<string> { "Platform account status must be active and approved before publication." }
            );
        }

        if (!readiness.IsReady)
        {
            var missing = readiness.Requirements
                .Where(r => r.IsMandatory && !r.IsSatisfied)
                .Select(r => r.Title)
                .ToList();

            return new PublishStorefrontResultDto(
                Success: false,
                IsPublished: false,
                StorefrontUrl: string.Empty,
                Message: "Storefront publication readiness check failed. Mandatory operational requirements must be satisfied before public discoverability.",
                MissingRequirements: missing
            );
        }

        // Section 28 Step 13: Publishing evaluates actual readiness and authorized permissions
        tenant.IsPublished = true;
        tenant.IsOnboardingCompleted = true;
        tenant.OnboardingStep = 13;
        tenant.IsActive = true;

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.Id,
            Actor = "ProviderOwner",
            Action = "Storefront.Published",
            Target = tenant.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                tenant.Name,
                tenant.Slug,
                tenant.IsPublished,
                tenant.IsOnboardingCompleted
            })
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new PublishStorefrontResultDto(
            Success: true,
            IsPublished: true,
            StorefrontUrl: $"/business/{tenant.Slug}",
            Message: "Provider storefront published successfully! Your business is now discoverable on Bookline marketplace."
        );
    }

    public async Task<OnboardingStatusDto> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

        if (tenant == null) throw new NotFoundException("Tenant", _tenantContext.TenantId);

        tenant.IsOnboardingCompleted = true;
        tenant.IsActive = true;
        tenant.OnboardingStep = 13;

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(tenant, cancellationToken);
    }

    private async Task<OnboardingStatusDto> MapToDtoAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenant.Id && !l.IsArchived, cancellationToken);

        var service = await _context.Services.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

        var product = await _context.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.IsActive, cancellationToken);

        var staff = await _context.Staff.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && !s.IsArchived, cancellationToken);

        var workingHours = await _context.WorkingHours.IgnoreQueryFilters()
            .Where(w => w.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        var ownerUser = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && (u.Role == "Owner" || u.Role == "Provider"), cancellationToken);

        var membership = ownerUser != null
            ? await _context.OrganizationMemberships.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.TenantId == tenant.Id && m.UserId == ownerUser.Id && m.IsActive, cancellationToken)
            : null;

        // Dynamically compute completed steps from saved records
        var completedSteps = new List<int> { 1 }; // Account is authenticated

        if (!string.IsNullOrWhiteSpace(tenant.Name)) completedSteps.Add(2);
        if (!string.IsNullOrWhiteSpace(tenant.BusinessType)) completedSteps.Add(3);
        if (!string.IsNullOrWhiteSpace(tenant.Description)) completedSteps.Add(4);
        if (location != null && !string.IsNullOrWhiteSpace(location.Address)) completedSteps.Add(5);
        if (!string.IsNullOrWhiteSpace(tenant.TimeZoneId) && !string.IsNullOrWhiteSpace(tenant.Currency)) completedSteps.Add(6);
        if (service != null && service.IsActive) completedSteps.Add(7);
        if (product != null && product.IsActive) completedSteps.Add(8);
        if (staff != null && staff.IsActive) completedSteps.Add(9);
        if (workingHours.Any()) completedSteps.Add(10);
        if (tenant.HoldDurationMinutes > 0 && tenant.BookingHorizonDays > 0) completedSteps.Add(11);
        if (tenant.IsOnboardingCompleted) completedSteps.Add(12);
        if (tenant.IsPublished) completedSteps.Add(13);

        var firstHours = workingHours.FirstOrDefault();

        return new OnboardingStatusDto(
            TenantId: tenant.Id,
            BusinessName: tenant.Name,
            BusinessSlug: tenant.Slug,
            BusinessType: tenant.BusinessType,
            Category: tenant.Category,
            Description: tenant.Description,
            Website: tenant.Website,
            Phone: tenant.Phone,
            Address: location?.Address ?? tenant.Address,
            City: location?.City ?? tenant.City,
            State: location?.State ?? tenant.State,
            PostalCode: location?.PostalCode ?? tenant.PostalCode,
            Country: location?.Country ?? tenant.Country,
            TimeZoneId: tenant.TimeZoneId,
            Currency: tenant.Currency,
            CurrentStep: tenant.OnboardingStep,
            IsCompleted: tenant.IsOnboardingCompleted,
            IsPublished: tenant.IsPublished,
            VerificationStatus: tenant.VerificationStatus.ToString(),
            ModerationStatus: tenant.ModerationStatus.ToString(),
            FirstServiceName: service?.Name,
            FirstServiceDurationMinutes: service?.DurationMinutes,
            FirstServicePrice: service?.Price,
            FirstProductName: product?.Name,
            FirstProductPrice: product?.Price,
            FirstProductStock: product?.StockQuantity,
            FirstProductSku: product?.Sku,
            FirstStaffName: staff?.Name,
            FirstStaffTitle: staff?.Title,
            StartTime: firstHours?.StartTime.ToString("HH:mm"),
            EndTime: firstHours?.EndTime.ToString("HH:mm"),
            HoldDurationMinutes: tenant.HoldDurationMinutes,
            MinimumNoticeHours: tenant.MinimumNoticeHours,
            BookingHorizonDays: tenant.BookingHorizonDays,
            CancellationWindowHours: 4,
            CompletedSteps: completedSteps,
            OwnerName: ownerUser?.FullName,
            OwnerEmail: ownerUser?.Email,
            OwnerPhone: ownerUser?.Phone,
            MembershipRole: membership?.Role ?? (ownerUser != null ? "Owner" : null)
        );
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return $"business-{Guid.NewGuid().ToString("N")[..6]}";
        var slug = Regex.Replace(text.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"business-{Guid.NewGuid().ToString("N")[..6]}" : slug;
    }
}
