namespace Bookline.Application.Availability.DTOs;

public record TimeSlotDto(
    string StartIso,
    string EndIso,
    string DisplayTime,
    bool IsAvailable,
    Guid? StaffId,
    string? StaffName
);

public record GetAvailabilitySlotsRequest(
    Guid ServiceId,
    Guid? StaffId,
    string Date, // YYYY-MM-DD
    string Timezone = "UTC"
);
