using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Services.Commands;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Services.Handlers;

public record UpdateServiceCommand(
    Guid Id,
    string Name,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price,
    bool IsActive
) : IRequest<ServiceDto>;

public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto>;

public record GetServicesQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<ServiceDto>>;

public class ServiceCommandHandler :
    IRequestHandler<CreateServiceCommand, ServiceDto>,
    IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IApplicationDbContext _dbContext;

    public ServiceCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DurationMinutes = request.DurationMinutes,
            BufferMinutes = request.BufferMinutes,
            Price = request.Price,
            IsActive = true
        };

        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceDto(service.Id, service.Name, service.DurationMinutes, service.BufferMinutes, service.Price, service.IsActive);
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), request.Id);
        }

        service.Name = request.Name;
        service.DurationMinutes = request.DurationMinutes;
        service.BufferMinutes = request.BufferMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceDto(service.Id, service.Name, service.DurationMinutes, service.BufferMinutes, service.Price, service.IsActive);
    }
}

public class ServiceQueryHandler :
    IRequestHandler<GetServiceByIdQuery, ServiceDto>,
    IRequestHandler<GetServicesQuery, PagedResult<ServiceDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public ServiceQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
        {
            throw new NotFoundException(nameof(Service), request.Id);
        }

        return new ServiceDto(service.Id, service.Name, service.DurationMinutes, service.BufferMinutes, service.Price, service.IsActive);
    }

    public async Task<PagedResult<ServiceDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery(request.Page, request.PageSize);

        var query = _dbContext.Services.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .Select(s => new ServiceDto(s.Id, s.Name, s.DurationMinutes, s.BufferMinutes, s.Price, s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceDto>(items, totalCount, pagedQuery.Page, pagedQuery.PageSize);
    }
}
