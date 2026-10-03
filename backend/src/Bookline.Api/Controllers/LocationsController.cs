using Bookline.Application.Locations.Commands;
using Bookline.Application.Locations.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ISender _sender;

    public LocationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> GetLocations([FromQuery] bool includeArchived = false)
    {
        var result = await _sender.Send(new GetLocationsQuery(includeArchived));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LocationDto>> GetLocationById(Guid id)
    {
        var result = await _sender.Send(new GetLocationByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationRequest request)
    {
        var result = await _sender.Send(new CreateLocationCommand(request));
        return CreatedAtAction(nameof(GetLocationById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LocationDto>> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var result = await _sender.Send(new UpdateLocationCommand(id, request));
        return Ok(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult> ArchiveLocation(Guid id)
    {
        await _sender.Send(new ArchiveLocationCommand(id));
        return NoContent();
    }
}
