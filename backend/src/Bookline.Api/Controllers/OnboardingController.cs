using System.Security.Claims;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITeamAuthorizationService _teamAuth;
    private readonly ITenantContext _tenantContext;
    private readonly IApplicationDbContext? _dbContext;

    public OnboardingController(
        ISender sender,
        ITeamAuthorizationService teamAuth,
        ITenantContext tenantContext,
        IApplicationDbContext? dbContext = null)
    {
        _sender = sender;
        _teamAuth = teamAuth;
        _tenantContext = tenantContext;
        _dbContext = dbContext;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<Guid> GetEffectiveTenantIdAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId != Guid.Empty)
        {
            return _tenantContext.TenantId;
        }

        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty || _dbContext == null) return Guid.Empty;

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == callerId, cancellationToken);

        return user?.TenantId ?? Guid.Empty;
    }

    private async Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty) return false;
        var tenantId = await GetEffectiveTenantIdAsync(cancellationToken);
        if (tenantId == Guid.Empty) return false;

        return await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken)
            || User.IsInRole("Admin")
            || User.IsInRole("PlatformAdmin")
            || User.IsInRole("Owner");
    }

    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty || _dbContext == null) return Unauthorized();

        var tenantId = await GetEffectiveTenantIdAsync(cancellationToken);
        if (tenantId == Guid.Empty)
        {
            var user = await _dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == callerId, cancellationToken);

            return Ok(new OnboardingStatusDto(
                Guid.Empty,
                "",
                "",
                "Salon",
                "Beauty & Wellness",
                "",
                null,
                null,
                "",
                "Ahmedabad",
                "Gujarat",
                "",
                "India",
                "Asia/Kolkata",
                "INR",
                1,
                false,
                false,
                "Pending",
                "Pending",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                5,
                2,
                30,
                4,
                new List<int> { 1 },
                user != null ? $"{user.FirstName} {user.LastName}".Trim() : "",
                user?.Email ?? "",
                null,
                "Owner"
            ));
        }

        var canManage = await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken);
        if (!canManage && !User.IsInRole("Admin") && !User.IsInRole("PlatformAdmin") && !User.IsInRole("Owner"))
        {
            return Forbid();
        }

        var result = await _sender.Send(new GetOnboardingStatusQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("step")]
    public async Task<ActionResult<OnboardingStatusDto>> SaveStep([FromBody] SaveOnboardingStepRequest request, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty || _dbContext == null) return Unauthorized();

        var tenantId = await GetEffectiveTenantIdAsync(cancellationToken);
        if (tenantId == Guid.Empty)
        {
            var user = await _dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == callerId, cancellationToken);
            if (user == null) return Unauthorized();

            var businessName = !string.IsNullOrWhiteSpace(request.BusinessName) ? request.BusinessName.Trim() : (user.FirstName + "'s Studio");
            var baseSlug = !string.IsNullOrWhiteSpace(request.BusinessSlug) ? request.BusinessSlug.Trim().ToLowerInvariant() : businessName.ToLowerInvariant().Replace(" ", "-");
            var cleanSlug = System.Text.RegularExpressions.Regex.Replace(baseSlug, @"[^a-z0-9\-]", "");
            if (string.IsNullOrWhiteSpace(cleanSlug)) cleanSlug = "studio-" + Guid.NewGuid().ToString("N")[..6];

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = businessName,
                Slug = cleanSlug,
                City = request.City ?? "Ahmedabad",
                Category = request.Category ?? request.BusinessType ?? "Beauty & Wellness",
                TimeZoneId = request.TimeZoneId ?? "Asia/Kolkata",
                Currency = request.Currency ?? "INR",
                IsActive = true,
                IsPublished = false,
                VerificationStatus = VerificationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            user.TenantId = tenant.Id;
            user.Role = "Owner";

            var membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = "Owner",
                IsActive = true,
                JoinedAtUtc = DateTime.UtcNow
            };

            _dbContext.Tenants.Add(tenant);
            _dbContext.OrganizationMemberships.Add(membership);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _tenantContext.SetTenantId(tenant.Id);
        }
        else
        {
            var canManage = await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken);
            if (!canManage && !User.IsInRole("Admin") && !User.IsInRole("PlatformAdmin") && !User.IsInRole("Owner"))
            {
                return Forbid();
            }
        }

        var result = await _sender.Send(new SaveOnboardingStepCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateTenant([FromBody] InitiateTenantRequest request, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty || _dbContext == null) return Unauthorized();

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == callerId, cancellationToken);
        if (user == null) return Unauthorized();

        var slug = request.Slug?.Trim().ToLowerInvariant() ?? request.BusinessName.Trim().ToLowerInvariant().Replace(" ", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

        var exists = await _dbContext.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug, cancellationToken);
        if (exists)
        {
            return Conflict(new { Code = "SLUG_ALREADY_EXISTS", Message = "The specified business URL is already taken." });
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.BusinessName.Trim(),
            Slug = slug,
            City = request.City ?? "Ahmedabad",
            Category = request.Category ?? "Beauty & Wellness",
            TimeZoneId = request.Timezone ?? "Asia/Kolkata",
            Currency = request.Currency ?? "INR",
            IsActive = true,
            IsPublished = false,
            VerificationStatus = VerificationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.TenantId = tenant.Id;
        user.Role = "Owner";

        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = "Owner",
            IsActive = true,
            JoinedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.OrganizationMemberships.Add(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Message = "Tenant initiated successfully."
        });
    }

    [HttpGet("readiness")]
    public async Task<ActionResult<OnboardingReadinessDto>> GetReadiness(CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken))
        {
            return Forbid();
        }

        var result = await _sender.Send(new GetOnboardingReadinessQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("publish")]
    public async Task<ActionResult<PublishStorefrontResultDto>> Publish(CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken))
        {
            return Forbid();
        }

        var result = await _sender.Send(new PublishStorefrontCommand(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<OnboardingStatusDto>> Complete(CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken))
        {
            return Forbid();
        }

        var result = await _sender.Send(new CompleteOnboardingCommand(), cancellationToken);
        return Ok(result);
    }
}

public record InitiateTenantRequest(string BusinessName, string? Slug = null, string? City = null, string? Category = null, string? Timezone = null, string? Currency = null);

