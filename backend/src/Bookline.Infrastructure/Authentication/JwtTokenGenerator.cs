using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Bookline.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:Secret"] ?? "SuperSecretKeyForBooklineApiThatIsAtLeast32BytesLong!";
        _issuer = configuration["JwtSettings:Issuer"] ?? "Bookline";
        _audience = configuration["JwtSettings:Audience"] ?? "BooklineApp";
    }

    public AccessTokenResult GenerateAccessToken(AppUser user, Guid? overrideTenantId = null, string? overrideRole = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var expiryUtc = DateTime.UtcNow.AddMinutes(60);
        var activeTenantId = overrideTenantId ?? user.TenantId;
        var activeRole = overrideRole ?? user.Role;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("tenant_id", activeTenantId.ToString()),
            new(ClaimTypes.Role, activeRole),
            new("role", activeRole),
            new("name", user.FullName)
        };

        // Ensure both canonical MVP role and legacy alias are present for policy evaluation
        if (Bookline.Domain.Constants.Roles.IsBusinessOwner(activeRole))
        {
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == Bookline.Domain.Constants.Roles.BusinessOwner))
                claims.Add(new Claim(ClaimTypes.Role, Bookline.Domain.Constants.Roles.BusinessOwner));
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == Bookline.Domain.Constants.Roles.Owner))
                claims.Add(new Claim(ClaimTypes.Role, Bookline.Domain.Constants.Roles.Owner));
        }
        else if (Bookline.Domain.Constants.Roles.IsBusinessStaff(activeRole))
        {
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == Bookline.Domain.Constants.Roles.BusinessStaff))
                claims.Add(new Claim(ClaimTypes.Role, Bookline.Domain.Constants.Roles.BusinessStaff));
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == Bookline.Domain.Constants.Roles.Staff))
                claims.Add(new Claim(ClaimTypes.Role, Bookline.Domain.Constants.Roles.Staff));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiryUtc,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AccessTokenResult(tokenHandler.WriteToken(token), expiryUtc);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string rawToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
