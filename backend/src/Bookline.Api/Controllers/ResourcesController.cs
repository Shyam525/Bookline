using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/resources")]
[Route("api/provider/resources")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly ISender _sender;

    public ResourcesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists bookable resources for the authorized business.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ResourceDto>>> GetResources(
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _sender.Send(new GetResourcesQuery(locationId, type, isActive));
        return Ok(result);
    }

    /// <summary>
    /// Gets resource details by resource ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> GetResourceById(Guid id)
    {
        var result = await _sender.Send(new GetResourceByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new bookable resource (treatment room, styling chair, machine, bay).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> CreateResource([FromBody] CreateResourceRequest request)
    {
        var result = await _sender.Send(new CreateResourceCommand(request));
        return CreatedAtAction(nameof(GetResourceById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing bookable resource.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> UpdateResource(Guid id, [FromBody] UpdateResourceRequest request)
    {
        var result = await _sender.Send(new UpdateResourceCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Deletes a bookable resource if it is not currently required by active services.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteResource(Guid id)
    {
        await _sender.Send(new DeleteResourceCommand(id));
        return NoContent();
    }
}
