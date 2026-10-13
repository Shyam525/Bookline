using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using FluentValidation;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Bookline.Application.Services.Handlers;

public class ServiceHandlers :
    IRequestHandler<GetServiceCategoriesQuery, List<ServiceCategoryDto>>,
    IRequestHandler<CreateServiceCategoryCommand, ServiceCategoryDto>,
    IRequestHandler<UpdateServiceCategoryCommand, ServiceCategoryDto>,
    IRequestHandler<DeleteServiceCategoryCommand, bool>,
    IRequestHandler<GetServicesQuery, List<ServiceDto>>,
    IRequestHandler<GetServiceByIdQuery, ServiceDto>,
    IRequestHandler<GetServiceBySlugQuery, ServiceDto>,
    IRequestHandler<CreateServiceCommand, ServiceDto>,
    IRequestHandler<UpdateServiceCommand, ServiceDto>,
    IRequestHandler<ArchiveServiceCommand, bool>,
    IRequestHandler<DuplicateServiceCommand, ServiceDto>,
    IRequestHandler<AssignServiceStaffCommand, ServiceDto>,
    IRequestHandler<AssignServiceLocationsCommand, ServiceDto>,
    IRequestHandler<SetServicePublishStatusCommand, ServiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ServiceHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    // --- Service Categories ---

    public async Task<List<ServiceCategoryDto>> Handle(GetServiceCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapCategoryToDto).ToList();
    }

    public async Task<ServiceCategoryDto> Handle(CreateServiceCategoryCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = req.Name.Trim(),
            Description = req.Description,
            SortOrder = req.SortOrder,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return MapCategoryToDto(category);
    }

    public async Task<ServiceCategoryDto> Handle(UpdateServiceCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category == null) throw new NotFoundException("ServiceCategory", command.Id);

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        category.Name = req.Name.Trim();
        category.Description = req.Description;
        category.SortOrder = req.SortOrder;
        category.IsActive = req.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapCategoryToDto(category);
    }

    public async Task<bool> Handle(DeleteServiceCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category == null) throw new NotFoundException("ServiceCategory", command.Id);

        if (category.Services.Any(s => !s.IsArchived))
        {
            throw new ValidationException("Cannot delete category containing active services. Move or archive services first.");
        }

        _context.ServiceCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // --- Services ---

    public async Task<List<ServiceDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == request.CategoryId.Value);
        }

        var services = await query
            .OrderBy(s => s.Category != null ? s.Category.SortOrder : 0)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return services.Select(MapServiceToDto).ToList();
    }

    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null) throw new NotFoundException("Service", request.Id);

        return MapServiceToDto(service);
    }

    public async Task<ServiceDto> Handle(GetServiceBySlugQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Slug == request.Slug, cancellationToken);

        if (service == null) throw new NotFoundException("Service", request.Slug);

        return MapServiceToDto(service);
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        ValidateServiceRequest(req.Name, req.DurationMinutes, req.BufferBeforeMinutes, req.BufferAfterMinutes, req.Price, req.Currency);

        var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("ServiceCategory", req.CategoryId);
        }

        var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : Slugify(req.Slug);
        var slugConflict = await _context.Services.AnyAsync(s => s.Slug == slug, cancellationToken);
        if (slugConflict)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString("n")[..4]}";
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = req.CategoryId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description,
            DurationMinutes = req.DurationMinutes,
            BufferBeforeMinutes = req.BufferBeforeMinutes,
            BufferAfterMinutes = req.BufferAfterMinutes,
            Price = req.Price,
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpperInvariant(),
            IsActive = true,
            IsOnlineBookingEnabled = req.IsOnlineBookingEnabled,
            IsPublished = req.IsPublished,
            Status = req.IsPublished ? "Active" : "Draft",
            IsArchived = false,
            ColorHex = req.ColorHex ?? "#E8546A",
            RequiresResource = req.RequiresResource,
            RequiredResourceType = req.RequiredResourceType,
            ImageUrl = req.ImageUrl,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Services.Add(service);

        // Assign staff if provided
        if (req.StaffIds != null && req.StaffIds.Count > 0)
        {
            var validStaffIds = await _context.Staff
                .Where(st => req.StaffIds.Contains(st.Id))
                .Select(st => st.Id)
                .ToListAsync(cancellationToken);

            if (validStaffIds.Count != req.StaffIds.Count)
            {
                throw new ValidationException("One or more assigned staff members are invalid or belong to another organization.");
            }

            foreach (var staffId in validStaffIds)
            {
                _context.StaffServices.Add(new StaffService
                {
                    TenantId = _tenantContext.TenantId,
                    StaffId = staffId,
                    ServiceId = service.Id
                });
            }
        }

        // Assign locations if provided
        if (req.LocationIds != null && req.LocationIds.Count > 0)
        {
            var validLocationIds = await _context.Locations
                .Where(loc => req.LocationIds.Contains(loc.Id))
                .Select(loc => loc.Id)
                .ToListAsync(cancellationToken);

            if (validLocationIds.Count != req.LocationIds.Count)
            {
                throw new ValidationException("One or more assigned locations are invalid or belong to another organization.");
            }

            foreach (var locId in validLocationIds)
            {
                _context.ServiceLocations.Add(new ServiceLocation
                {
                    TenantId = _tenantContext.TenantId,
                    ServiceId = service.Id,
                    LocationId = locId,
                    IsAvailable = true
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload category, staff and locations for DTO mapping
        var savedService = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(savedService);
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (service == null) throw new NotFoundException("Service", command.Id);

        var req = command.Request;
        ValidateServiceRequest(req.Name, req.DurationMinutes, req.BufferBeforeMinutes, req.BufferAfterMinutes, req.Price, req.Currency);

        if (service.CategoryId != req.CategoryId)
        {
            var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new NotFoundException("ServiceCategory", req.CategoryId);
            }
        }

        if (!string.IsNullOrWhiteSpace(req.Slug) && req.Slug != service.Slug)
        {
            var newSlug = Slugify(req.Slug);
            var slugConflict = await _context.Services.AnyAsync(s => s.Id != service.Id && s.Slug == newSlug, cancellationToken);
            if (slugConflict)
            {
                newSlug = $"{newSlug}-{Guid.NewGuid().ToString("n")[..4]}";
            }
            service.Slug = newSlug;
        }
        else if (string.IsNullOrWhiteSpace(service.Slug))
        {
            service.Slug = Slugify(req.Name);
        }

        service.CategoryId = req.CategoryId;
        service.Name = req.Name.Trim();
        service.Description = req.Description;
        service.DurationMinutes = req.DurationMinutes;
        service.BufferBeforeMinutes = req.BufferBeforeMinutes;
        service.BufferAfterMinutes = req.BufferAfterMinutes;
        service.Price = req.Price;
        service.Currency = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpperInvariant();
        service.IsActive = req.IsActive;
        service.IsOnlineBookingEnabled = req.IsOnlineBookingEnabled;
        service.IsPublished = req.IsPublished;
        service.Status = req.IsActive ? (req.IsPublished ? "Active" : "Draft") : "Inactive";
        service.ColorHex = req.ColorHex ?? "#E8546A";
        service.RequiresResource = req.RequiresResource;
        service.RequiredResourceType = req.RequiredResourceType;
        if (req.ImageUrl != null) service.ImageUrl = req.ImageUrl;
        service.UpdatedAtUtc = DateTime.UtcNow;

        if (req.StaffIds != null)
        {
            var validStaffIds = await _context.Staff
                .Where(st => req.StaffIds.Contains(st.Id))
                .Select(st => st.Id)
                .ToListAsync(cancellationToken);

            if (validStaffIds.Count != req.StaffIds.Count)
            {
                throw new ValidationException("One or more assigned staff members are invalid or belong to another organization.");
            }

            var existingStaff = await _context.StaffServices.Where(ss => ss.ServiceId == service.Id).ToListAsync(cancellationToken);
            _context.StaffServices.RemoveRange(existingStaff);

            foreach (var staffId in validStaffIds)
            {
                _context.StaffServices.Add(new StaffService
                {
                    TenantId = _tenantContext.TenantId,
                    StaffId = staffId,
                    ServiceId = service.Id
                });
            }
        }

        if (req.LocationIds != null)
        {
            var validLocationIds = await _context.Locations
                .Where(loc => req.LocationIds.Contains(loc.Id))
                .Select(loc => loc.Id)
                .ToListAsync(cancellationToken);

            if (validLocationIds.Count != req.LocationIds.Count)
            {
                throw new ValidationException("One or more assigned locations are invalid or belong to another organization.");
            }

            var existingLocs = await _context.ServiceLocations.Where(sl => sl.ServiceId == service.Id).ToListAsync(cancellationToken);
            _context.ServiceLocations.RemoveRange(existingLocs);

            foreach (var locId in validLocationIds)
            {
                _context.ServiceLocations.Add(new ServiceLocation
                {
                    TenantId = _tenantContext.TenantId,
                    ServiceId = service.Id,
                    LocationId = locId,
                    IsAvailable = true
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updatedService = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(updatedService);
    }

    public async Task<bool> Handle(ArchiveServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (service == null) throw new NotFoundException("Service", command.Id);

        service.IsArchived = true;
        service.IsActive = false;
        service.Status = "Archived";
        service.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ServiceDto> Handle(DuplicateServiceCommand command, CancellationToken cancellationToken)
    {
        var original = await _context.Services
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (original == null) throw new NotFoundException("Service", command.Id);

        var duplicate = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = original.CategoryId,
            Name = $"{original.Name} (Copy)",
            Slug = $"{original.Slug}-copy-{Guid.NewGuid().ToString("n")[..4]}",
            Description = original.Description,
            DurationMinutes = original.DurationMinutes,
            BufferBeforeMinutes = original.BufferBeforeMinutes,
            BufferAfterMinutes = original.BufferAfterMinutes,
            Price = original.Price,
            Currency = original.Currency,
            IsActive = original.IsActive,
            IsOnlineBookingEnabled = original.IsOnlineBookingEnabled,
            IsPublished = original.IsPublished,
            Status = original.Status,
            IsArchived = false,
            ColorHex = original.ColorHex,
            RequiresResource = original.RequiresResource,
            RequiredResourceType = original.RequiredResourceType,
            ImageUrl = original.ImageUrl,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Services.Add(duplicate);

        foreach (var ss in original.StaffServices)
        {
            _context.StaffServices.Add(new StaffService
            {
                TenantId = _tenantContext.TenantId,
                StaffId = ss.StaffId,
                ServiceId = duplicate.Id
            });
        }

        foreach (var sl in original.ServiceLocations)
        {
            _context.ServiceLocations.Add(new ServiceLocation
            {
                TenantId = _tenantContext.TenantId,
                ServiceId = duplicate.Id,
                LocationId = sl.LocationId,
                IsAvailable = sl.IsAvailable,
                PriceOverride = sl.PriceOverride
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var savedDuplicate = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstAsync(s => s.Id == duplicate.Id, cancellationToken);

        return MapServiceToDto(savedDuplicate);
    }

    public async Task<ServiceDto> Handle(AssignServiceStaffCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == command.ServiceId, cancellationToken);

        if (service == null) throw new NotFoundException("Service", command.ServiceId);

        var validStaffIds = await _context.Staff
            .Where(st => command.StaffIds.Contains(st.Id))
            .Select(st => st.Id)
            .ToListAsync(cancellationToken);

        if (validStaffIds.Count != command.StaffIds.Count)
        {
            throw new ValidationException("One or more assigned staff members are invalid or belong to another organization.");
        }

        var existing = await _context.StaffServices.Where(ss => ss.ServiceId == service.Id).ToListAsync(cancellationToken);
        _context.StaffServices.RemoveRange(existing);

        foreach (var sid in validStaffIds)
        {
            _context.StaffServices.Add(new StaffService
            {
                TenantId = _tenantContext.TenantId,
                StaffId = sid,
                ServiceId = service.Id
            });
        }

        service.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var reloaded = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(reloaded);
    }

    public async Task<ServiceDto> Handle(AssignServiceLocationsCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == command.ServiceId, cancellationToken);

        if (service == null) throw new NotFoundException("Service", command.ServiceId);

        var validLocationIds = await _context.Locations
            .Where(loc => command.LocationIds.Contains(loc.Id))
            .Select(loc => loc.Id)
            .ToListAsync(cancellationToken);

        if (validLocationIds.Count != command.LocationIds.Count)
        {
            throw new ValidationException("One or more assigned locations are invalid or belong to another organization.");
        }

        var existing = await _context.ServiceLocations.Where(sl => sl.ServiceId == service.Id).ToListAsync(cancellationToken);
        _context.ServiceLocations.RemoveRange(existing);

        foreach (var lid in validLocationIds)
        {
            _context.ServiceLocations.Add(new ServiceLocation
            {
                TenantId = _tenantContext.TenantId,
                ServiceId = service.Id,
                LocationId = lid,
                IsAvailable = true
            });
        }

        service.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var reloaded = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(reloaded);
    }

    public async Task<ServiceDto> Handle(SetServicePublishStatusCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.StaffServices)
            .Include(s => s.ServiceLocations)
            .FirstOrDefaultAsync(s => s.Id == command.ServiceId, cancellationToken);

        if (service == null) throw new NotFoundException("Service", command.ServiceId);

        service.IsPublished = command.IsPublished;
        service.Status = command.IsPublished ? "Active" : "Draft";
        service.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapServiceToDto(service);
    }

    // --- Helpers ---

    private static void ValidateServiceRequest(string name, int durationMinutes, int bufferBefore, int bufferAfter, decimal price, string currency)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Service name is required.");
        if (durationMinutes <= 0)
            throw new ValidationException("Duration must be greater than 0 minutes.");
        if (bufferBefore < 0)
            throw new ValidationException("Buffer before must be non-negative.");
        if (bufferAfter < 0)
            throw new ValidationException("Buffer after must be non-negative.");
        if (price < 0)
            throw new ValidationException("Price must be non-negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException("Currency is required.");
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("n")[..8];
        var slug = text.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }

    private static ServiceCategoryDto MapCategoryToDto(ServiceCategory category)
    {
        return new ServiceCategoryDto(
            category.Id,
            category.TenantId,
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive,
            category.Services?.Count(s => !s.IsArchived) ?? 0
        );
    }

    private static ServiceDto MapServiceToDto(Service service)
    {
        return new ServiceDto(
            service.Id,
            service.TenantId,
            service.CategoryId,
            service.Category?.Name ?? "Uncategorized",
            service.Name,
            service.Slug,
            service.Description,
            service.DurationMinutes,
            service.BufferBeforeMinutes,
            service.BufferAfterMinutes,
            service.TotalDurationMinutes,
            service.Price,
            service.Currency,
            service.IsActive,
            service.IsOnlineBookingEnabled,
            service.IsPublished,
            service.Status,
            service.IsArchived,
            service.ColorHex,
            service.RequiresResource,
            service.RequiredResourceType,
            service.ImageUrl,
            service.StaffServices?.Select(s => s.StaffId).ToList() ?? new List<Guid>(),
            service.ServiceLocations?.Select(l => l.LocationId).ToList() ?? new List<Guid>(),
            service.CreatedAtUtc,
            service.UpdatedAtUtc
        );
    }
}
