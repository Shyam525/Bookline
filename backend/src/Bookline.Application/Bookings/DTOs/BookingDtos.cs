namespace Bookline.Application.Bookings.DTOs;

using Bookline.Domain.Entities;

public record BookingDto(
    Guid Id,
    Guid TenantId,
    Guid StaffId,
    Guid ServiceId,
    Guid CustomerId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    BookingStatus Status,
    DateTimeOffset CreatedAtUtc,
    string? BookingReference = null,
    Guid? RelatedOrderId = null,
    string? RelatedOrderNumber = null,
    string? ServiceName = null,
    string? StaffName = null,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? CustomerEmail = null,
    decimal Price = 0.00m,
    string Currency = "INR",
    string Source = "Manual",
    string? CustomerNotes = null,
    string? InternalNotes = null,
    string? TimeZoneId = null,
    Guid? LocationId = null
);

public record HoldSlotResultDto(
    Guid HoldId,
    DateTimeOffset ExpiresAtUtc,
    string? Slot = null,
    string Status = "HELD"
)
{
    public DateTimeOffset ExpiresAt => ExpiresAtUtc;
}
