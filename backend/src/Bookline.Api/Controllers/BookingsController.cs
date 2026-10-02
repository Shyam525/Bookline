using Bookline.Application.Bookings.Queries;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize(Policy = "ManageBookings")]
public class BookingsController : ControllerBase
{
    private readonly ISender _sender;

    public BookingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarBookingDto>>> GetCalendar(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        [FromQuery] Guid? staffId,
        CancellationToken cancellationToken)
    {
        if (toUtc <= fromUtc || toUtc - fromUtc > TimeSpan.FromDays(31))
        {
            return BadRequest("The calendar range must be positive and no longer than 31 days.");
        }

        var bookings = await _sender.Send(
            new GetCalendarBookingsQuery(fromUtc, toUtc, staffId),
            cancellationToken);

        return Ok(bookings);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelBookingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ConfirmBookingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<ActionResult<BookingDto>> Reschedule(
        Guid id,
        [FromBody] RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _sender.Send(
            new RescheduleBookingCommand(id, request.StartUtc, request.HoldId),
            cancellationToken);
        return Ok(booking);
    }
}

public record RescheduleBookingRequest(DateTimeOffset StartUtc, Guid HoldId);
