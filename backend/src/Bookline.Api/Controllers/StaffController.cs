using Bookline.Application.Staff.Commands;
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

    [HttpGet]
    public async Task<ActionResult<List<StaffDto>>> GetAllStaff([FromQuery] bool includeArchived = false)
    {
        var result = await _sender.Send(new GetAllStaffQuery(includeArchived));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffDto>> GetStaffById(Guid id)
    {
        var result = await _sender.Send(new GetStaffByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StaffDto>> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var result = await _sender.Send(new CreateStaffCommand(request));
        return CreatedAtAction(nameof(GetStaffById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StaffDto>> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
    {
        var result = await _sender.Send(new UpdateStaffCommand(id, request));
        return Ok(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult> ArchiveStaff(Guid id)
    {
        await _sender.Send(new ArchiveStaffCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}/services")]
    public async Task<ActionResult<StaffDto>> AssignStaffServices(Guid id, [FromBody] List<Guid> serviceIds)
    {
        var result = await _sender.Send(new AssignStaffServicesCommand(id, serviceIds));
        return Ok(result);
    }

    [HttpPut("{id:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourDto>>> SetWorkingHours(Guid id, [FromBody] List<WorkingHourInput> workingHours)
    {
        var result = await _sender.Send(new SetWorkingHoursCommand(id, workingHours));
        return Ok(result);
    }
}
