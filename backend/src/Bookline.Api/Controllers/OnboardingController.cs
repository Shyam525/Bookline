using System.Security.Claims;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITeamAuthorizationService _teamAuth;
    private readonly ITenantContext _tenantContext;

    public OnboardingController(
        ISender sender,
        ITeamAuthorizationService teamAuth,
        ITenantContext tenantContext)
    {
        _sender = sender;
        _teamAuth = teamAuth;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty) return false;
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) return false;

        return await _teamAuth.CanManageTeamAsync(callerId, tenantId, cancellationToken);
    }

    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken))
        {
            return Forbid();
        }

        var result = await _sender.Send(new GetOnboardingStatusQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("step")]
    public async Task<ActionResult<OnboardingStatusDto>> SaveStep([FromBody] SaveOnboardingStepRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken))
        {
            return Forbid();
        }

        var result = await _sender.Send(new SaveOnboardingStepCommand(request), cancellationToken);
        return Ok(result);
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
