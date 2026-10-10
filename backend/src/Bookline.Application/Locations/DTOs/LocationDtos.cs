namespace Bookline.Application.Locations.DTOs;

public record LocationDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string State,
    string PostalCode,
    double Latitude,
    double Longitude,
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
    string Currency,
    string City = "Ahmedabad",
    string State = "Gujarat",
    string PostalCode = "",
    double? Latitude = null,
    double? Longitude = null
);

public record UpdateLocationRequest(
    string Name,
    string Address,
    string Phone,
    string Timezone,
    string Currency,
    bool IsActive,
    string City = "Ahmedabad",
    string State = "Gujarat",
    string PostalCode = "",
    double? Latitude = null,
    double? Longitude = null
);
