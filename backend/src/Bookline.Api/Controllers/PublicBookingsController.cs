namespace Bookline.Api.Controllers;

using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NodaTime;
using NodaTime.Text;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
[EnableRateLimiting("booking-limit")]
public class PublicBookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly Bookline.Application.Common.Interfaces.IIdempotencyService _idempotencyService;

    public PublicBookingsController(
        IMediator mediator,
        Bookline.Application.Common.Interfaces.IIdempotencyService idempotencyService)
    {
        _mediator = mediator;
        _idempotencyService = idempotencyService;
    }

    [HttpGet("availability")]
    public async Task<ActionResult<IReadOnlyList<Slot>>> GetAvailability(
        [FromQuery] Guid staffId,
        [FromQuery] Guid serviceId,
        [FromQuery] string date,
        [FromQuery] string? timezone,
        [FromQuery] string? slug,
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
            timezone ?? "UTC",
            slug);

        var result = await _mediator.Send(query, cancellationToken);
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        options.Converters.Add(new InstantJsonConverter());
        return Content(System.Text.Json.JsonSerializer.Serialize(result, options), "application/json");
    }

    [HttpPost("hold")]
    [EnableRateLimiting("hold-limit")]
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
        var idempKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            var cached = await _idempotencyService.GetExistingAsync(idempKey, "CreateBooking", cancellationToken);
            if (cached != null)
            {
                var cachedDto = System.Text.Json.JsonSerializer.Deserialize<BookingDto>(cached.ResponseJson);
                if (cachedDto != null) return StatusCode(cached.StatusCode, cachedDto);
            }
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "CreateBooking",
                201,
                System.Text.Json.JsonSerializer.Serialize(result),
                result.TenantId,
                null,
                cancellationToken);
        }

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
