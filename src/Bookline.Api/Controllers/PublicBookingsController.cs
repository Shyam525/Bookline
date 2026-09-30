namespace Bookline.Api.Controllers;

using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
public class PublicBookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicBookingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("availability")]
    public async Task<ActionResult<IReadOnlyList<Slot>>> GetAvailability(
        [FromQuery] Guid staffId,
        [FromQuery] Guid serviceId,
        [FromQuery] string date,
        [FromQuery] string? timezone,
        CancellationToken cancellationToken)
    {
        var parseResult = LocalDatePattern.Iso.Parse(date);
        if (!parseResult.Success)
        {
            return BadRequest("Invalid date format. Expected YYYY-MM-DD.");
        }

        var query = new GetPublicAvailabilityQuery(
            staffId,
            serviceId,
            parseResult.Value,
            timezone ?? "UTC");

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("hold")]
    public async Task<ActionResult<HoldSlotResultDto>> HoldSlot(
        [FromBody] HoldSlotCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("book")]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        [FromBody] CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateBooking), new { id = result.Id }, result);
    }

    [HttpGet("tenant/{slug}")]
    public async Task<ActionResult<Bookline.Application.Tenants.Queries.PublicTenantDto>> GetTenantBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Bookline.Application.Tenants.Queries.GetPublicTenantBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }

    [HttpGet("token/validate")]
    public ActionResult ValidateActionToken(
        [FromQuery] string token,
        [FromServices] Bookline.Application.Common.Interfaces.IBookingActionTokenService tokenService)
    {
        var isValid = tokenService.TryValidateToken(token, out var bookingId, out var action);
        return Ok(new { valid = isValid, bookingId, action });
    }
}
