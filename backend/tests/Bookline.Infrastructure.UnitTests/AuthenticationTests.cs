using System.IdentityModel.Tokens.Jwt;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class AuthenticationTests
{
    [Fact]
    public void PasswordHasher_ShouldHashAndVerifyPasswordCorrectly()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "SecurePassword123!";

        // Act
        var hash = hasher.HashPassword(password);
        var isValid = hasher.VerifyPassword(password, hash);
        var isInvalid = hasher.VerifyPassword("WrongPassword!", hash);

        // Assert
        Assert.NotEqual(password, hash);
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateTokenWithTenantIdAndClaims()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "SuperSecretKeyForBooklineApiThatIsAtLeast32BytesLong!"},
            {"JwtSettings:Issuer", "Bookline"},
            {"JwtSettings:Audience", "BooklineApp"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var tokenGenerator = new JwtTokenGenerator(configuration);

        var tenantId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "owner@tenant.com",
            TenantId = tenantId,
            Role = "Owner"
        };

        // Act
        var result = tokenGenerator.GenerateAccessToken(user);

        // Read token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        // Assert
        Assert.NotNull(result.Token);
        Assert.True(result.ExpiryUtc > DateTime.UtcNow);
        Assert.Equal(user.Id.ToString(), jwtToken.Subject);
        Assert.Equal("owner@tenant.com", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == "email").Value);
        Assert.Equal(tenantId.ToString(), jwtToken.Claims.First(c => c.Type == "tenant_id").Value);
        Assert.Equal("Owner", jwtToken.Claims.First(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value);
    }

    [Fact]
    public void HashRefreshToken_ShouldProduceConsistentSha256Hash()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var tokenGenerator = new JwtTokenGenerator(configuration);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        // Act
        var hash1 = tokenGenerator.HashRefreshToken(refreshToken);
        var hash2 = tokenGenerator.HashRefreshToken(refreshToken);

        // Assert
        Assert.NotEmpty(refreshToken);
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(refreshToken, hash1);
    }

    [Fact]
    public void RefreshToken_ExpirationAndRevocation_ShouldBeTrackedCorrectly()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TokenHash = "sample-hash",
            ExpiryUtc = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false,
            IsUsed = false
        };

        // Assert
        Assert.True(refreshToken.ExpiryUtc < DateTime.UtcNow);
        
        refreshToken.IsRevoked = true;
        Assert.True(refreshToken.IsRevoked);
    }
}
