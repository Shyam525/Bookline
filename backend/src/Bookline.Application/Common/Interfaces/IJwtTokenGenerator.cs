using Bookline.Domain.Entities;

namespace Bookline.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiryUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(AppUser user, Guid? overrideTenantId = null, string? overrideRole = null);
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}
