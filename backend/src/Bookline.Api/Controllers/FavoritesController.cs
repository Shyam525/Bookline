using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;

    public FavoritesController(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid? GetCustomerId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var favorites = await _dbContext.Favorites
            .Where(f => f.CustomerId == customerId.Value)
            .Include(f => f.Tenant)
            .Select(f => new
            {
                f.Id,
                f.TenantId,
                f.CreatedAtUtc,
                Provider = f.Tenant != null ? new
                {
                    f.Tenant.Id,
                    f.Tenant.Name,
                    f.Tenant.Slug,
                    f.Tenant.Category,
                    f.Tenant.BusinessType,
                    f.Tenant.City,
                    f.Tenant.Address,
                    f.Tenant.AverageRating,
                    f.Tenant.ReviewCount,
                    f.Tenant.LogoUrl,
                    f.Tenant.CoverImageUrl
                } : null
            })
            .ToListAsync(cancellationToken);

        return Ok(favorites);
    }

    [HttpPost("{tenantId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid tenantId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var existing = await _dbContext.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId.Value && f.TenantId == tenantId, cancellationToken);

        if (existing != null) return Ok(existing);

        var fav = new Favorite
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId.Value,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Favorites.Add(fav);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(fav);
    }

    [HttpDelete("{tenantId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid tenantId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var existing = await _dbContext.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId.Value && f.TenantId == tenantId, cancellationToken);

        if (existing != null)
        {
            _dbContext.Favorites.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("check/{tenantId:guid}")]
    public async Task<IActionResult> CheckFavorite(Guid tenantId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Ok(new { IsFavorite = false });

        var exists = await _dbContext.Favorites
            .AnyAsync(f => f.CustomerId == customerId.Value && f.TenantId == tenantId, cancellationToken);

        return Ok(new { IsFavorite = exists });
    }
}
