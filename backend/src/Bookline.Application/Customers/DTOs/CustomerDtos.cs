namespace Bookline.Application.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    Guid TenantId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    string? Notes,
    string? AvatarUrl,
    int TotalBookingsCount,
    decimal TotalSpentAmount,
    bool IsArchived,
    DateTime CreatedAtUtc
);

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Notes = null,
    string? AvatarUrl = null
);

public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Notes = null,
    string? AvatarUrl = null
);
