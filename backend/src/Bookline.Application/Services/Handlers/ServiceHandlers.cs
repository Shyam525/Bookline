using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using FluentValidation;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Services.Handlers;

public class ServiceHandlers :
    IRequestHandler<GetServiceCategoriesQuery, List<ServiceCategoryDto>>,
    IRequestHandler<CreateServiceCategoryCommand, ServiceCategoryDto>,
    IRequestHandler<UpdateServiceCategoryCommand, ServiceCategoryDto>,
    IRequestHandler<DeleteServiceCategoryCommand, bool>,
    IRequestHandler<GetServicesQuery, List<ServiceDto>>,
    IRequestHandler<GetServiceByIdQuery, ServiceDto>,
    IRequestHandler<CreateServiceCommand, ServiceDto>,
    IRequestHandler<UpdateServiceCommand, ServiceDto>,
    IRequestHandler<ArchiveServiceCommand, bool>,
    IRequestHandler<DuplicateServiceCommand, ServiceDto>
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
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null) throw new NotFoundException("Service", request.Id);

        return MapServiceToDto(service);
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        ValidateServiceRequest(req.Name, req.DurationMinutes, req.BufferBeforeMinutes, req.BufferAfterMinutes, req.Price);

        var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("ServiceCategory", req.CategoryId);
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = req.CategoryId,
            Name = req.Name.Trim(),
            Description = req.Description,
            DurationMinutes = req.DurationMinutes,
            BufferBeforeMinutes = req.BufferBeforeMinutes,
            BufferAfterMinutes = req.BufferAfterMinutes,
            Price = req.Price,
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency,
            IsActive = true,
            IsOnlineBookingEnabled = req.IsOnlineBookingEnabled,
            IsArchived = false,
            ColorHex = req.ColorHex ?? "#E8546A",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload category for DTO mapping
        var savedService = await _context.Services
            .Include(s => s.Category)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(savedService);
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (service == null) throw new NotFoundException("Service", command.Id);

        var req = command.Request;
        ValidateServiceRequest(req.Name, req.DurationMinutes, req.BufferBeforeMinutes, req.BufferAfterMinutes, req.Price);

        if (service.CategoryId != req.CategoryId)
        {
            var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new NotFoundException("ServiceCategory", req.CategoryId);
            }
        }

        service.CategoryId = req.CategoryId;
        service.Name = req.Name.Trim();
        service.Description = req.Description;
        service.DurationMinutes = req.DurationMinutes;
        service.BufferBeforeMinutes = req.BufferBeforeMinutes;
        service.BufferAfterMinutes = req.BufferAfterMinutes;
        service.Price = req.Price;
        service.Currency = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency;
        service.IsActive = req.IsActive;
        service.IsOnlineBookingEnabled = req.IsOnlineBookingEnabled;
        service.ColorHex = req.ColorHex ?? "#E8546A";
        service.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var updatedService = await _context.Services
            .Include(s => s.Category)
            .FirstAsync(s => s.Id == service.Id, cancellationToken);

        return MapServiceToDto(updatedService);
    }

    public async Task<bool> Handle(ArchiveServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (service == null) throw new NotFoundException("Service", command.Id);

        service.IsArchived = true;
        service.IsActive = false;
        service.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ServiceDto> Handle(DuplicateServiceCommand command, CancellationToken cancellationToken)
    {
        var original = await _context.Services.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (original == null) throw new NotFoundException("Service", command.Id);

        var duplicate = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = original.CategoryId,
            Name = $"{original.Name} (Copy)",
            Description = original.Description,
            DurationMinutes = original.DurationMinutes,
            BufferBeforeMinutes = original.BufferBeforeMinutes,
            BufferAfterMinutes = original.BufferAfterMinutes,
            Price = original.Price,
            Currency = original.Currency,
            IsActive = original.IsActive,
            IsOnlineBookingEnabled = original.IsOnlineBookingEnabled,
            IsArchived = false,
            ColorHex = original.ColorHex,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Services.Add(duplicate);
        await _context.SaveChangesAsync(cancellationToken);

        var savedDuplicate = await _context.Services
            .Include(s => s.Category)
            .FirstAsync(s => s.Id == duplicate.Id, cancellationToken);

        return MapServiceToDto(savedDuplicate);
    }

    // --- Helpers ---

    private static void ValidateServiceRequest(string name, int durationMinutes, int bufferBefore, int bufferAfter, decimal price)
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
            service.Description,
            service.DurationMinutes,
            service.BufferBeforeMinutes,
            service.BufferAfterMinutes,
            service.TotalDurationMinutes,
            service.Price,
            service.Currency,
            service.IsActive,
            service.IsOnlineBookingEnabled,
            service.IsArchived,
            service.ColorHex,
            service.CreatedAtUtc,
            service.UpdatedAtUtc
        );
    }
}
