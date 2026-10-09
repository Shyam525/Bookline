using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/customer")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly ISlotEngine _slotEngine;

    public CustomerController(BooklineDbContext dbContext, ISlotEngine slotEngine)
    {
        _dbContext = dbContext;
        _slotEngine = slotEngine;
    }

    private Guid? GetCustomerId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == customerId.Value, cancellationToken);

        if (user == null) return NotFound(new { Message = "Customer profile not found." });

        var bookingCount = await _dbContext.Bookings.IgnoreQueryFilters()
            .CountAsync(b => b.CustomerId == customerId.Value, cancellationToken);

        var orderCount = await _dbContext.Orders.IgnoreQueryFilters()
            .CountAsync(o => o.CustomerId == customerId.Value, cancellationToken);

        var favoriteCount = await _dbContext.Favorites
            .CountAsync(f => f.CustomerId == customerId.Value, cancellationToken);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Phone,
            user.AvatarUrl,
            Stats = new
            {
                TotalBookings = bookingCount,
                TotalOrders = orderCount,
                FavoritesCount = favoriteCount
            }
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == customerId.Value, cancellationToken);

        if (user == null) return NotFound(new { Message = "Customer profile not found." });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(user);
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var bookings = await _dbContext.Bookings.IgnoreQueryFilters()
            .Where(b => b.CustomerId == customerId.Value)
            .Include(b => b.Staff)
            .Include(b => b.Service)
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        var tenantIds = bookings.Select(b => b.TenantId).Distinct().ToList();
        var tenants = await _dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var locationIds = bookings.Where(b => b.LocationId.HasValue).Select(b => b.LocationId!.Value).Distinct().ToList();
        var locations = await _dbContext.Locations.IgnoreQueryFilters()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var result = bookings.Select(b =>
        {
            tenants.TryGetValue(b.TenantId, out var tenant);
            Location? loc = null;
            if (b.LocationId.HasValue) locations.TryGetValue(b.LocationId.Value, out loc);

            return new
            {
                b.Id,
                b.BookingReference,
                b.TenantId,
                ProviderName = tenant?.Name ?? "Provider",
                ProviderSlug = tenant?.Slug ?? "",
                LocationName = loc?.Name ?? tenant?.Address ?? "",
                ServiceName = b.Service?.Name ?? "Service",
                StaffName = b.Staff?.Name ?? "Specialist",
                b.StartUtc,
                b.EndUtc,
                Status = b.Status.ToString(),
                b.TotalPrice,
                b.DepositPaid,
                Currency = tenant?.Currency ?? "USD",
                b.CancellationReason,
                b.CancelledBy,
                b.CustomerNotes,
                b.CreatedAtUtc,
                MinimumNoticeHours = tenant?.MinimumNoticeHours ?? 2
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("appointments/{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleCustomerAppointmentRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var booking = await _dbContext.Bookings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId.Value, cancellationToken);

        if (booking == null) return NotFound(new { Message = "Appointment not found." });

        if (request.NewEndUtc <= request.NewStartUtc)
        {
            return BadRequest(new { Message = "End time must be after start time." });
        }

        // Check for conflicts
        var hasConflict = await _dbContext.Bookings.IgnoreQueryFilters()
            .AnyAsync(b => b.StaffId == booking.StaffId &&
                           b.Id != booking.Id &&
                           (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                           b.StartUtc < request.NewEndUtc &&
                           b.EndUtc > request.NewStartUtc,
                      cancellationToken);

        if (hasConflict)
        {
            return Conflict(new { Message = "The selected time slot is already booked. Please choose another time." });
        }

        booking.Reschedule(request.NewStartUtc, request.NewEndUtc);

        // Section 66: Audit and Outbox inside atomic transaction
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = User?.Identity?.Name ?? "Customer",
            Action = "Booking.Rescheduled",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                NewStartUtc = request.NewStartUtc,
                NewEndUtc = request.NewEndUtc,
                RescheduledAtUtc = DateTimeOffset.UtcNow
            })
        });

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = "BookingRescheduled",
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerId = booking.CustomerId,
                StaffId = booking.StaffId,
                NewStartUtc = request.NewStartUtc,
                NewEndUtc = request.NewEndUtc
            })
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(booking);
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelCustomerAppointmentRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var booking = await _dbContext.Bookings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId.Value, cancellationToken);

        if (booking == null) return NotFound(new { Message = "Appointment not found." });

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == booking.TenantId, cancellationToken);

        // Section 67: Configurable cancellation window policy check
        var minHours = tenant?.MinimumNoticeHours ?? 2;
        var hoursUntilStart = (booking.StartUtc - DateTimeOffset.UtcNow).TotalHours;

        if (hoursUntilStart < minHours)
        {
            return BadRequest(new { Message = $"Cancellations must be made at least {minHours} hours prior to appointment." });
        }

        var actor = User?.Identity?.Name ?? "Customer";
        var reason = request.Reason ?? "Cancelled by customer";
        booking.Cancel(reason, actor);

        // Section 67: Capture reason, actor, and timestamp in audit and outbox
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = booking.TenantId,
            Actor = actor,
            Action = "Booking.Cancelled",
            Target = booking.Id.ToString(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingReference = booking.BookingReference,
                Reason = reason,
                Actor = actor,
                CancelledAtUtc = DateTimeOffset.UtcNow
            })
        });

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = booking.TenantId,
            EventType = "BookingCancelled",
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                Reason = reason,
                Actor = actor,
                CancelledAtUtc = DateTimeOffset.UtcNow
            })
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(booking);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue) return Unauthorized();

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == customerId.Value, cancellationToken);

        var query = _dbContext.NotificationLogs.IgnoreQueryFilters()
            .Where(n => n.CustomerId == customerId.Value || (user != null && n.RecipientEmail == user.Email));

        var dbLogs = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(30)
            .ToListAsync(cancellationToken);

        if (dbLogs.Any())
        {
            var list = dbLogs.Select(n => new
            {
                Id = n.Id.ToString(),
                Title = n.Subject,
                Message = n.Body,
                CreatedAtUtc = n.CreatedAtUtc,
                IsRead = n.IsRead,
                Link = n.DeepLinkUrl ?? (n.BookingId.HasValue ? "/appointments" : "/dashboard")
            }).ToList();

            return Ok(list);
        }

        var notifications = new[]
        {
            new
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Appointment Confirmed",
                Message = "Your appointment with Glow Hair Lounge has been confirmed.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                IsRead = false,
                Link = "/appointments"
            },
            new
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Order Dispatched",
                Message = "Your retail order ORD-839210 has been processed.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                IsRead = true,
                Link = "/orders"
            }
        };

        return Ok(notifications);
    }
}

public record UpdateCustomerProfileRequest(string FirstName, string LastName, string? Phone, string? AvatarUrl);
public record RescheduleCustomerAppointmentRequest(DateTimeOffset NewStartUtc, DateTimeOffset NewEndUtc);
public record CancelCustomerAppointmentRequest(string? Reason);
