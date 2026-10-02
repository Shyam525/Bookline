using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthController(
        BooklineDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    [HttpPost("register-tenant")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { Message = "Email and password are required." });
        }

        var existingUser = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.OwnerEmail.ToLower());

        if (existingUser != null)
        {
            return BadRequest(new { Message = "User with this email already exists." });
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.TenantName,
            Slug = request.TenantSlug,
            CreatedAtUtc = DateTime.UtcNow
        };

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.OwnerEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Owner",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);

        var tokenResult = _tokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _tokenGenerator.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiryUtc = DateTime.UtcNow.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = tenant.Id,
            TokenHash = hashedRefreshToken,
            ExpiryUtc = refreshTokenExpiryUtc,
            IsRevoked = false,
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse(
            tokenResult.Token,
            tokenResult.ExpiryUtc,
            rawRefreshToken,
            refreshTokenExpiryUtc,
            user.Id,
            tenant.Id,
            user.Email,
            user.Role
        ));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var tokenResult = _tokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _tokenGenerator.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiryUtc = DateTime.UtcNow.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            TokenHash = hashedRefreshToken,
            ExpiryUtc = refreshTokenExpiryUtc,
            IsRevoked = false,
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse(
            tokenResult.Token,
            tokenResult.ExpiryUtc,
            rawRefreshToken,
            refreshTokenExpiryUtc,
            user.Id,
            user.TenantId,
            user.Email,
            user.Role
        ));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { Message = "Refresh token is required." });
        }

        var hashedInputToken = _tokenGenerator.HashRefreshToken(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedInputToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.IsUsed || existingToken.ExpiryUtc < DateTime.UtcNow)
        {
            return Unauthorized(new { Message = "Invalid, expired, or reused refresh token." });
        }

        existingToken.IsUsed = true;

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId);

        if (user == null)
        {
            return Unauthorized(new { Message = "User associated with token not found." });
        }

        var tokenResult = _tokenGenerator.GenerateAccessToken(user);
        var newRawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var newHashedRefreshToken = _tokenGenerator.HashRefreshToken(newRawRefreshToken);
        var refreshTokenExpiryUtc = DateTime.UtcNow.AddDays(7);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            TokenHash = newHashedRefreshToken,
            ExpiryUtc = refreshTokenExpiryUtc,
            IsRevoked = false,
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse(
            tokenResult.Token,
            tokenResult.ExpiryUtc,
            newRawRefreshToken,
            refreshTokenExpiryUtc,
            user.Id,
            user.TenantId,
            user.Email,
            user.Role
        ));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hashedInputToken = _tokenGenerator.HashRefreshToken(request.RefreshToken);
            var existingToken = await _dbContext.RefreshTokens.IgnoreQueryFilters()
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedInputToken);

            if (existingToken != null)
            {
                existingToken.IsRevoked = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        return Ok(new { Message = "Logged out successfully." });
    }
}
