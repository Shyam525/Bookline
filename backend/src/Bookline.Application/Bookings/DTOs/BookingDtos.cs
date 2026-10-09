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
    DateTimeOffset CreatedAtUtc
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
