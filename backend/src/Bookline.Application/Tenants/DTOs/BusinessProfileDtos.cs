using System.Text.Json.Serialization;

namespace Bookline.Application.Tenants.DTOs;

public record BusinessProfileDto(
    Guid BusinessId,
    string Name,
    string Slug,
    string Description,
    string Category,
    string BusinessType,
    string? Phone,
    string? Email,
    string? Website,
    string Address,
    string City,
    string Area,
    string State,
    string Country,
    string PostalCode,
    double Latitude,
    double Longitude,
    string TimeZoneId,
    string Currency,
    bool IsPublished,
    bool IsActive,
    string? LogoUrl,
    string? CoverImageUrl,
    List<string> GalleryImages,
    Guid? PrimaryLocationId,
    string? PrimaryLocationName
);

public record UpdateBusinessProfileRequest(
    string Name,
    string Slug,
    string Description,
    string Category,
    string BusinessType,
    string? Phone,
    string? Email,
    string? Website,
    string Address,
    string City,
    string Area,
    string State,
    string Country,
    string PostalCode,
    double? Latitude,
    double? Longitude,
    string TimeZoneId,
    string? LogoUrl,
    string? CoverImageUrl,
    List<string>? GalleryImages,
    bool? IsPublished
);

public record BusinessBreakPeriod(TimeOnly StartTime, TimeOnly EndTime, string? Label = null);

public record BusinessDayHours(
    DayOfWeek DayOfWeek,
    bool IsOpen,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime,
    List<BusinessBreakPeriod>? Breaks = null
);

public record BusinessPoliciesDto(
    int HoldDurationMinutes,
    int MinimumNoticeHours,
    int BookingHorizonDays,
    int CancellationWindowHours,
    string? CancellationPolicy,
    int ReschedulingWindowHours,
    string? ReschedulingPolicy,
    List<BusinessDayHours> WeeklyHours
);

public record UpdateBusinessPoliciesRequest(
    int HoldDurationMinutes,
    int MinimumNoticeHours,
    int BookingHorizonDays,
    int CancellationWindowHours,
    string? CancellationPolicy,
    int ReschedulingWindowHours,
    string? ReschedulingPolicy,
    List<BusinessDayHours>? WeeklyHours = null
);

public record CheckSlugAvailabilityResponse(bool IsAvailable, string Slug, string? Message = null);

