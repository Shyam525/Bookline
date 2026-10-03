namespace Bookline.Application.Tenants.DTOs;

public record OnboardingStatusDto(
    Guid TenantId,
    string BusinessName,
    string BusinessSlug,
    string BusinessType,
    string Address,
    string TimeZoneId,
    string Currency,
    int CurrentStep,
    bool IsCompleted,
    int HoldDurationMinutes,
    int MinimumNoticeHours,
    int BookingHorizonDays
);

public record SaveOnboardingStepRequest(
    int Step,
    string? BusinessName,
    string? BusinessType,
    string? Address,
    string? TimeZoneId,
    string? Currency,
    string? FirstServiceName,
    int? FirstServiceDurationMinutes,
    decimal? FirstServicePrice,
    string? FirstStaffName,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? HoldDurationMinutes,
    int? MinimumNoticeHours,
    int? BookingHorizonDays
);
