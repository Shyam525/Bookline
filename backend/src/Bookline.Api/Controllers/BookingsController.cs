using Bookline.Application.Bookings.Queries;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize]
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
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelBookingRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var actor = User.Identity?.Name ?? "Customer";
        await _sender.Send(new CancelBookingCommand(id, request?.Reason, actor), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ConfirmBookingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateBookingStatusCommand(id, "CheckedIn"), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateBookingStatusCommand(id, "Completed"), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateBookingStatusCommand(id, "NoShow"), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateBookingStatusCommand(id, request.Status, request.Reason), cancellationToken);
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
public record CancelBookingRequest(string? Reason = null);
public record UpdateBookingStatusRequest(string Status, string? Reason = null);
