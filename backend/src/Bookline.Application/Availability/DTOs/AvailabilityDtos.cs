namespace Bookline.Application.Availability.DTOs;

public record TimeSlotDto(
    string StartIso,
    string EndIso,
    string DisplayTime,
    bool IsAvailable,
    Guid? StaffId,
    string? StaffName,
    string Date = "",
    string LocalTime = "",
    string Instant = "",
    string Timezone = "UTC",
    int DurationMinutes = 0
);

public record GetAvailabilitySlotsRequest(
    Guid ServiceId,
    Guid? StaffId,
    string Date, // YYYY-MM-DD
    string Timezone = "UTC"
);
