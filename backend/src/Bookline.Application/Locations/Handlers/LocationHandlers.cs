using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Locations.Commands;
using Bookline.Application.Locations.DTOs;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Locations.Handlers;

public class LocationHandlers :
    IRequestHandler<GetLocationsQuery, List<LocationDto>>,
    IRequestHandler<GetLocationByIdQuery, LocationDto>,
    IRequestHandler<CreateLocationCommand, LocationDto>,
    IRequestHandler<UpdateLocationCommand, LocationDto>,
    IRequestHandler<ArchiveLocationCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public LocationHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Locations.AsNoTracking();
        if (!request.IncludeArchived)
        {
            query = query.Where(l => !l.IsArchived);
        }

        var locations = await query.OrderBy(l => l.Name).ToListAsync(cancellationToken);
        return locations.Select(MapToDto).ToList();
    }

    public async Task<LocationDto> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (location == null) throw new NotFoundException("Location", request.Id);

        return MapToDto(location);
    }

    public async Task<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = req.Name,
            Address = req.Address,
            Phone = req.Phone,
            Timezone = req.Timezone,
            Currency = req.Currency,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(location);
    }

    public async Task<LocationDto> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);

        if (location == null) throw new NotFoundException("Location", command.Id);

        location.Name = req.Name;
        location.Address = req.Address;
        location.Phone = req.Phone;
        location.Timezone = req.Timezone;
        location.Currency = req.Currency;
        location.IsActive = req.IsActive;
        location.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(location);
    }

    public async Task<bool> Handle(ArchiveLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);
        if (location == null) throw new NotFoundException("Location", command.Id);

        location.IsArchived = true;
        location.IsActive = false;
        location.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LocationDto MapToDto(Location l)
    {
        return new LocationDto(
            l.Id,
            l.TenantId,
            l.Name,
            l.Address,
            l.Phone,
            l.Timezone,
            l.Currency,
            l.IsActive,
            l.IsArchived,
            l.CreatedAtUtc
        );
    }
}
