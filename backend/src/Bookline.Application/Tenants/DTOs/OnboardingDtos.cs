namespace Bookline.Application.Tenants.DTOs;

public record OnboardingStatusDto(
    Guid TenantId,
    string BusinessName,
    string BusinessSlug,
    string BusinessType,
    string Category,
    string Description,
    string? Website,
    string? Phone,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Country,
    string TimeZoneId,
    string Currency,
    int CurrentStep,
    bool IsCompleted,
    bool IsPublished,
    string VerificationStatus,
    string ModerationStatus,
    string? FirstServiceName,
    int? FirstServiceDurationMinutes,
    decimal? FirstServicePrice,
    string? FirstProductName,
    decimal? FirstProductPrice,
    int? FirstProductStock,
    string? FirstProductSku,
    string? FirstStaffName,
    string? FirstStaffTitle,
    string? StartTime,
    string? EndTime,
    int HoldDurationMinutes,
    int MinimumNoticeHours,
    int BookingHorizonDays,
    int CancellationWindowHours,
    List<int> CompletedSteps
);

public record SaveOnboardingStepRequest(
    int Step,
    string? BusinessName = null,
    string? BusinessSlug = null,
    string? BusinessType = null,
    string? Category = null,
    string? Description = null,
    string? Website = null,
    string? Phone = null,
    string? LocationName = null,
    string? Address = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null,
    string? TimeZoneId = null,
    string? Currency = null,
    string? FirstServiceName = null,
    int? FirstServiceDurationMinutes = null,
    decimal? FirstServicePrice = null,
    string? FirstProductName = null,
    decimal? FirstProductPrice = null,
    int? FirstProductStock = null,
    string? FirstProductSku = null,
    string? FirstStaffName = null,
    string? FirstStaffTitle = null,
    string? FirstStaffEmail = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    List<int>? WorkingDays = null,
    int? HoldDurationMinutes = null,
    int? MinimumNoticeHours = null,
    int? BookingHorizonDays = null,
    int? CancellationWindowHours = null
);

public record OnboardingReadinessRequirement(
    string Key,
    string Title,
    string Description,
    int StepNumber,
    bool IsSatisfied,
    bool IsMandatory,
    string ActionRoute
);

public record OnboardingReadinessDto(
    bool IsReady,
    int TotalMandatory,
    int SatisfiedMandatory,
    bool AllMandatorySatisfied,
    List<OnboardingReadinessRequirement> Requirements
);

public record PublishStorefrontResultDto(
    bool Success,
    bool IsPublished,
    string StorefrontUrl,
    string Message,
    List<string>? MissingRequirements = null
);
