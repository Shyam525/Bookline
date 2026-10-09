using System.Security.Claims;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/team")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ITeamAuthorizationService _teamAuth;

    public TeamController(
        BooklineDbContext dbContext,
        ITenantContext tenantContext,
        ITeamAuthorizationService teamAuth)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _teamAuth = teamAuth;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [HttpGet("members")]
    public async Task<IActionResult> GetTeamMembers(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var memberships = await _dbContext.OrganizationMemberships
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = await _dbContext.Users.IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = memberships.Select(m =>
        {
            users.TryGetValue(m.UserId, out var u);
            return new
            {
                m.Id,
                m.UserId,
                Name = u?.FullName ?? "Team Specialist",
                Email = u?.Email ?? string.Empty,
                Role = m.Role,
                m.JoinedAtUtc,
                AvatarUrl = (string?)null
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteTeamMember([FromBody] InviteTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        var tenantId = _tenantContext.TenantId;

        // Server-enforced permissions (Section 95)
        if (!await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken))
        {
            return Forbid();
        }

        if (!Enum.TryParse<ProviderTeamRole>(request.Role, true, out var role))
        {
            return BadRequest(new { Message = "Invalid team role. Valid roles: Owner, Admin, Manager, Receptionist, Staff, Viewer." });
        }

        var existingUser = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        var memberUserId = existingUser?.Id ?? Guid.NewGuid();
        if (existingUser == null)
        {
            existingUser = new AppUser
            {
                Id = memberUserId,
                TenantId = tenantId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role,
                PasswordHash = "INVITED_UNSET",
                CreatedAtUtc = DateTime.UtcNow
            };
            _dbContext.Users.Add(existingUser);
        }

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == memberUserId && m.TenantId == tenantId, cancellationToken);

        if (membership != null)
        {
            membership.Role = role.ToString();
        }
        else
        {
            membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = memberUserId,
                Role = role.ToString(),
                JoinedAtUtc = DateTime.UtcNow
            };
            _dbContext.OrganizationMemberships.Add(membership);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            membership.Id,
            membership.UserId,
            Name = $"{request.FirstName} {request.LastName}".Trim(),
            Email = request.Email,
            Role = membership.Role,
            membership.JoinedAtUtc
        });
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, [FromBody] UpdateMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        var tenantId = _tenantContext.TenantId;

        // Server-enforced permissions (Section 95)
        if (!await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken))
        {
            return Forbid();
        }

        if (!Enum.TryParse<ProviderTeamRole>(request.Role, true, out var role))
        {
            return BadRequest(new { Message = "Invalid role. Valid: Owner, Admin, Manager, Receptionist, Staff, Viewer." });
        }

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId, cancellationToken);

        if (membership == null)
        {
            return NotFound(new { Message = "Team member not found." });
        }

        membership.Role = role.ToString();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { membership.Id, membership.Role });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        var tenantId = _tenantContext.TenantId;

        // Server-enforced permissions (Section 95)
        if (!await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken))
        {
            return Forbid();
        }

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId, cancellationToken);

        if (membership == null)
        {
            return NotFound(new { Message = "Team member not found." });
        }

        _dbContext.OrganizationMemberships.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record InviteTeamMemberRequest(string Email, string FirstName, string LastName, string Role);
public record UpdateMemberRoleRequest(string Role);
