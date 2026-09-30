using Bookline.Application.Common.Models;
using Bookline.Application.Staff.Commands;
using Bookline.Application.Staff.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly ISender _sender;

    public StaffController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = "ManageStaff")]
    public async Task<ActionResult<StaffDto>> Create([FromBody] CreateStaffCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/working-hours")]
    [Authorize(Policy = "ManageStaff")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourDto>>> SetWorkingHours(Guid id, [FromBody] List<WorkingHourInput> workingHours)
    {
        var result = await _sender.Send(new SetWorkingHoursCommand(id, workingHours));
        return Ok(result);
    }

    [HttpPost("{id:guid}/time-off")]
    [Authorize(Policy = "ManageStaff")]
    public async Task<ActionResult<TimeOffDto>> CreateTimeOff(Guid id, [FromBody] CreateTimeOffCommand command)
    {
        if (id != command.StaffId)
        {
            return BadRequest(new { Message = "URL staff ID does not match request body staff ID." });
        }

        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<StaffDto>> GetById(Guid id)
    {
        var result = await _sender.Send(new GetStaffByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<PagedResult<StaffDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetStaffQuery(page, pageSize));
        return Ok(result);
    }
}
