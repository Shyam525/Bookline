using Bookline.Application.Availability.Commands;
using Bookline.Application.Availability.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly ISender _sender;

    public AvailabilityController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("slots")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TimeSlotDto>>> GetSlots(
        [FromQuery] Guid serviceId,
        [FromQuery] Guid? staffId = null,
        [FromQuery] string date = "",
        [FromQuery] string timezone = "UTC")
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        var result = await _sender.Send(new GetAvailabilitySlotsQuery(serviceId, staffId, date, timezone));
        return Ok(result);
    }
}
