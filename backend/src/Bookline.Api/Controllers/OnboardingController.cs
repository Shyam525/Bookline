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

    public OnboardingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus()
    {
        var result = await _sender.Send(new GetOnboardingStatusQuery());
        return Ok(result);
    }

    [HttpPost("step")]
    public async Task<ActionResult<OnboardingStatusDto>> SaveStep([FromBody] SaveOnboardingStepRequest request)
    {
        var result = await _sender.Send(new SaveOnboardingStepCommand(request));
        return Ok(result);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<OnboardingStatusDto>> Complete()
    {
        var result = await _sender.Send(new CompleteOnboardingCommand());
        return Ok(result);
    }
}
