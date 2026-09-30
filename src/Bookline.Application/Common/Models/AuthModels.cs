namespace Bookline.Application.Common.Models;

public record RegisterTenantRequest(
    string TenantName,
    string TenantSlug,
    string OwnerEmail,
    string Password,
    string FirstName,
    string LastName
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiryUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiryUtc,
    Guid UserId,
    Guid TenantId,
    string Email,
    string Role
);
