using Bookline.Application.Common.Models;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = "ManageServices")]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManageServices")]
    public async Task<ActionResult<ServiceDto>> Update(Guid id, [FromBody] UpdateServiceCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { Message = "URL ID does not match request body ID." });
        }

        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<ServiceDto>> GetById(Guid id)
    {
        var result = await _sender.Send(new GetServiceByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetServicesQuery(page, pageSize));
        return Ok(result);
    }
}
