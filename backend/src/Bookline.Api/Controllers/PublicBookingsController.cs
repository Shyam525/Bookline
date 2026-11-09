namespace Bookline.Api.Controllers;

using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
[EnableRateLimiting("booking-limit")]
public class PublicBookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IBookingActionTokenService _tokenService;

    public PublicBookingsController(
        IMediator mediator,
        IIdempotencyService idempotencyService,
        IBookingActionTokenService tokenService)
    {
        _mediator = mediator;
        _idempotencyService = idempotencyService;
        _tokenService = tokenService;
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

        // Generate secure 90-day guest management action token (Phase 5)
        var manageToken = _tokenService.GenerateToken(result.Id, "manage", TimeSpan.FromDays(90));
        var enrichedResult = result with
        {
            ManageToken = manageToken,
            PublicManageUrl = $"/manage-booking?token={Uri.EscapeDataString(manageToken)}"
        };

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "CreateBooking",
                201,
                System.Text.Json.JsonSerializer.Serialize(enrichedResult),
                enrichedResult.TenantId,
                null,
                cancellationToken);
        }

        return CreatedAtAction(nameof(CreateBooking), new { id = enrichedResult.Id }, enrichedResult);
    }

    /// <summary>
    /// Secure guest appointment lookup via cryptographically signed opaque token.
    /// Prevents enumeration/guessing attacks on appointment IDs.
    /// </summary>
    [HttpGet("bookings/manage")]
    public async Task<IActionResult> GetGuestBooking(
        [FromQuery] string token,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] IClock clock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokenService.TryValidateToken(token, out var bookingId, out _))
        {
            return Unauthorized(new ApiErrorResponse(
                "INVALID_TOKEN",
                "The booking management token is invalid, tampered with, or expired.",
                HttpContext.TraceIdentifier,
                401));
        }

        var booking = await dbContext.Bookings.IgnoreQueryFilters()
            .Include(b => b.Staff)
            .Include(b => b.Service)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            return NotFound(new ApiErrorResponse("BOOKING_NOT_FOUND", "Appointment not found.", HttpContext.TraceIdentifier, 404));
        }

        var tenant = await dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == booking.TenantId, cancellationToken);

        Location? loc = null;
        if (booking.LocationId.HasValue)
        {
            loc = await dbContext.Locations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == booking.LocationId.Value, cancellationToken);
        }

        var minNoticeHours = tenant?.MinimumNoticeHours ?? 2;
        var nowInstant = clock.GetCurrentInstant();
        var startInstant = NodaTime.Instant.FromDateTimeOffset(booking.StartUtc);
        var canCancel = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed;
        var isWithinCancellationWindow = canCancel && (startInstant >= nowInstant + NodaTime.Duration.FromHours(minNoticeHours));

        return Ok(new
        {
            booking.Id,
            booking.BookingReference,
            booking.TenantId,
            ProviderName = tenant?.Name ?? "Provider",
            ProviderSlug = tenant?.Slug ?? "",
            LocationName = loc?.Name ?? tenant?.Address ?? "",
            ServiceName = booking.Service?.Name ?? booking.ServiceNameSnapshot ?? "Service",
            StaffName = booking.Staff?.Name ?? "Specialist",
            CustomerName = booking.Customer?.FullName ?? "Guest Customer",
            CustomerEmail = booking.Customer?.Email,
            CustomerPhone = booking.Customer?.Phone,
            booking.StartUtc,
            booking.EndUtc,
            Status = booking.Status.ToString(),
            booking.TotalPrice,
            booking.DepositPaid,
            Currency = tenant?.Currency ?? "INR",
            booking.CustomerNotes,
            booking.CreatedAtUtc,
            MinimumNoticeHours = minNoticeHours,
            CanCancel = isWithinCancellationWindow,
            CanReschedule = canCancel,
            CancellationPolicy = tenant?.CancellationPolicy ?? "Standard Cancellation Policy"
        });
    }

    /// <summary>
    /// Cancel guest appointment via secure action token, verifying the provider's cancellation window policy.
    /// </summary>
    [HttpPost("bookings/cancel")]
    public async Task<IActionResult> CancelGuestBooking(
        [FromBody] CancelGuestBookingRequest request,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] IClock clock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || !_tokenService.TryValidateToken(request.Token, out var bookingId, out _))
        {
            return Unauthorized(new ApiErrorResponse(
                "INVALID_TOKEN",
                "The booking management token is invalid, tampered with, or expired.",
                HttpContext.TraceIdentifier,
                401));
        }

        var booking = await dbContext.Bookings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            return NotFound(new ApiErrorResponse("BOOKING_NOT_FOUND", "Appointment not found.", HttpContext.TraceIdentifier, 404));
        }

        var tenant = await dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == booking.TenantId, cancellationToken);

        var minHours = tenant?.MinimumNoticeHours ?? 2;
        var nowInstant = clock.GetCurrentInstant();
        var startInstant = NodaTime.Instant.FromDateTimeOffset(booking.StartUtc);
        if (startInstant < nowInstant + NodaTime.Duration.FromHours(minHours))
        {
            return BadRequest(new ApiErrorResponse(
                "CANCELLATION_WINDOW_EXPIRED",
                $"Appointments cannot be cancelled within {minHours} hours of the scheduled start time.",
                HttpContext.TraceIdentifier,
                400));
        }

        booking.Cancel("GuestCustomer", request.Reason ?? "Cancelled by customer");

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = "GuestCustomer",
            Action = "Booking.CancelledByGuest",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                booking.BookingReference,
                Reason = request.Reason
            })
        });

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = Bookline.Domain.Constants.NotificationEvents.AppointmentCancelled,
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                booking.BookingReference,
                booking.TenantId,
                booking.CustomerId,
                booking.StaffId,
                booking.ServiceId,
                booking.StartUtc,
                Reason = request.Reason
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Appointment cancelled successfully.", status = "Cancelled" });
    }

    /// <summary>
    /// Reschedule guest appointment via secure action token, verifying schedule eligibility with Phase 4 engine.
    /// </summary>
    [HttpPost("bookings/reschedule")]
    public async Task<IActionResult> RescheduleGuestBooking(
        [FromBody] RescheduleGuestBookingRequest request,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] ISlotHoldService slotHoldService,
        [FromServices] IAppointmentSchedulingService schedulingService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || !_tokenService.TryValidateToken(request.Token, out var bookingId, out _))
        {
            return Unauthorized(new ApiErrorResponse(
                "INVALID_TOKEN",
                "The booking management token is invalid, tampered with, or expired.",
                HttpContext.TraceIdentifier,
                401));
        }

        var command = new RescheduleBookingCommand(bookingId, request.NewStartUtc, request.HoldId, request.StaffId);
        var handler = new RescheduleBookingCommandHandler(dbContext, slotHoldService, schedulingService);
        var result = await handler.Handle(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Legacy tenant metadata lookup by slug.
    /// Note: Canonical rich storefront profiles with services, products, locations, and reviews
    /// are served authoritatively by <see cref="PublicBusinessesController.GetBusinessBySlug"/>.
    /// </summary>
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
        [FromServices] IBookingActionTokenService tokenService)
    {
        var isValid = tokenService.TryValidateToken(token, out var bookingId, out var action);
        return Ok(new { valid = isValid, bookingId, action });
    }
}

public record CancelGuestBookingRequest(string Token, string? Reason = null);
public record RescheduleGuestBookingRequest(string Token, DateTimeOffset NewStartUtc, Guid? HoldId = null, Guid? StaffId = null);
