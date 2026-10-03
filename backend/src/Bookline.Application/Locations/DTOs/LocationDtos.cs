namespace Bookline.Application.Locations.DTOs;

public record LocationDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Address,
    string Phone,
    string Timezone,
    string Currency,
    bool IsActive,
    bool IsArchived,
    DateTime CreatedAtUtc
);

public record CreateLocationRequest(
    string Name,
    string Address,
    string Phone,
    string Timezone,
    string Currency
);

public record UpdateLocationRequest(
    string Name,
    string Address,
    string Phone,
    string Timezone,
    string Currency,
    bool IsActive
);
