namespace Bookline.Application.Services.DTOs;

public record ServiceCategoryDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive,
    int ServicesCount
);

public record CreateServiceCategoryRequest(
    string Name,
    string? Description = null,
    int SortOrder = 0
);

public record UpdateServiceCategoryRequest(
    string Name,
    string? Description = null,
    int SortOrder = 0,
    bool IsActive = true
);

public record ServiceDto(
    Guid Id,
    Guid TenantId,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    int DurationMinutes,
    int BufferBeforeMinutes,
    int BufferAfterMinutes,
    int TotalDurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    bool IsOnlineBookingEnabled,
    bool IsPublished,
    string Status,
    bool IsArchived,
    string? ColorHex,
    bool RequiresResource,
    string? RequiredResourceType,
    string? ImageUrl,
    List<Guid> StaffIds,
    List<Guid> LocationIds,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid? RequiredResourceId = null,
    string? RequiredResourceName = null
);

public record CreateServiceRequest(
    Guid CategoryId,
    string Name,
    string? Slug = null,
    string? Description = null,
    int DurationMinutes = 30,
    int BufferBeforeMinutes = 0,
    int BufferAfterMinutes = 0,
    decimal Price = 0.00m,
    string Currency = "USD",
    bool IsOnlineBookingEnabled = true,
    bool IsPublished = true,
    string? ColorHex = "#E8546A",
    bool RequiresResource = false,
    string? RequiredResourceType = null,
    Guid? RequiredResourceId = null,
    string? ImageUrl = null,
    List<Guid>? StaffIds = null,
    List<Guid>? LocationIds = null
);

public record UpdateServiceRequest(
    Guid CategoryId,
    string Name,
    string? Slug = null,
    string? Description = null,
    int DurationMinutes = 30,
    int BufferBeforeMinutes = 0,
    int BufferAfterMinutes = 0,
    decimal Price = 0.00m,
    string Currency = "USD",
    bool IsActive = true,
    bool IsOnlineBookingEnabled = true,
    bool IsPublished = true,
    string? ColorHex = "#E8546A",
    bool RequiresResource = false,
    string? RequiredResourceType = null,
    Guid? RequiredResourceId = null,
    string? ImageUrl = null,
    List<Guid>? StaffIds = null,
    List<Guid>? LocationIds = null
);

public record SetServiceStatusRequest(string Status);

public record ConfigureServiceResourcesRequest(
    bool RequiresResource,
    string? RequiredResourceType = null,
    Guid? RequiredResourceId = null
);

public record LocationPriceOverrideDto(
    Guid LocationId,
    decimal? PriceOverride = null,
    bool IsAvailable = true
);

public record ResourceDto(
    Guid Id,
    Guid TenantId,
    Guid? LocationId,
    string? LocationName,
    string Name,
    string Description,
    string Type,
    int Capacity,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateResourceRequest(
    string Name,
    string? Description = null,
    string Type = "Room",
    int Capacity = 1,
    Guid? LocationId = null
);

public record UpdateResourceRequest(
    string Name,
    string? Description = null,
    string Type = "Room",
    int Capacity = 1,
    bool IsActive = true,
    Guid? LocationId = null
);

