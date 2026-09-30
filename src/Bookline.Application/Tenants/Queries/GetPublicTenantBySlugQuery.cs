namespace Bookline.Application.Tenants.Queries;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record PublicServiceDto(Guid Id, string Name, int DurationMinutes, int BufferMinutes, decimal Price);
public record PublicStaffDto(Guid Id, string Name, string TimeZoneId);
public record PublicTenantDto(Guid Id, string Name, string Slug, IReadOnlyList<PublicServiceDto> Services, IReadOnlyList<PublicStaffDto> Staff);

public record GetPublicTenantBySlugQuery(string Slug) : IRequest<PublicTenantDto>;

public class GetPublicTenantBySlugQueryHandler : IRequestHandler<GetPublicTenantBySlugQuery, PublicTenantDto>
{
    private readonly IApplicationDbContext _context;

    public GetPublicTenantBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicTenantDto> Handle(GetPublicTenantBySlugQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug.ToLower() == request.Slug.ToLower() && t.IsActive, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException($"Tenant with slug '{request.Slug}' was not found.");
        }

        var services = await _context.Services
            .AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.IsActive)
            .Select(s => new PublicServiceDto(s.Id, s.Name, s.DurationMinutes, s.BufferMinutes, s.Price))
            .ToListAsync(cancellationToken);

        var staff = await _context.Staff
            .AsNoTracking()
            .Where(st => st.TenantId == tenant.Id && st.IsActive)
            .Select(st => new PublicStaffDto(st.Id, st.Name, st.TimeZoneId))
            .ToListAsync(cancellationToken);

        return new PublicTenantDto(tenant.Id, tenant.Name, tenant.Slug, services, staff);
    }
}
