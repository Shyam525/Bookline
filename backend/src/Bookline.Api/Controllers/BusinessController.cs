using Bookline.Application.Common.Interfaces;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/business")]
[Route("api/provider/business")]
[Authorize]
public class BusinessController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITeamAuthorizationService _teamAuth;
    private readonly ITenantContext _tenantContext;

    public BusinessController(
        ISender sender,
        ITeamAuthorizationService teamAuth,
        ITenantContext tenantContext)
    {
        _sender = sender;
        _teamAuth = teamAuth;
        _tenantContext = tenantContext;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<BusinessProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBusinessProfileQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<BusinessProfileDto>> UpdateProfile(
        [FromBody] UpdateBusinessProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateBusinessProfileCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("policies")]
    public async Task<ActionResult<BusinessPoliciesDto>> GetPolicies(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBusinessPoliciesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("policies")]
    public async Task<ActionResult<BusinessPoliciesDto>> UpdatePolicies(
        [FromBody] UpdateBusinessPoliciesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateBusinessPoliciesCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("slug/check")]
    public async Task<ActionResult<CheckSlugAvailabilityResponse>> CheckSlug(
        [FromQuery] string slug,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckSlugAvailabilityQuery(slug), cancellationToken);
        return Ok(result);
    }
}

