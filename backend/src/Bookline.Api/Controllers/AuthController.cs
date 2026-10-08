using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;

    public AuthController(
        BooklineDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IEmailSender emailSender)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Email address is required." });
        }

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user != null)
        {
            var resetToken = Guid.NewGuid().ToString("N");
            await _emailSender.SendEmailAsync(
                user.Email,
                "Bookline Password Reset Request",
                $"Use this token to reset your password: {resetToken}"
            );
        }

        // Return 200 OK regardless to prevent user enumeration
        return Ok(new { Message = "If the email is registered, password reset instructions have been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { Message = "Email and new password are required." });
        }

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            return BadRequest(new { Message = "Invalid password reset request." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = "Password reset successfully. You can now log in with your new password." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user token claims." });
        }

        var user = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { Message = "User profile not found." });
        }

        return Ok(new UserDto(
            user.Id,
            user.TenantId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role
        ));
    }

    [HttpPost("register-customer")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { Message = "Email and password are required." });
        }

        var existingUser = await _dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return BadRequest(new { Message = "User with this email already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = "Customer",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        var tokenResult = _tokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _tokenGenerator.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiryUtc = DateTime.UtcNow.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = Guid.Empty,
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
            Guid.Empty,
            user.Email,
            user.Role
        ));
    }

    [HttpGet("my-businesses")]
    [Authorize]
    public async Task<IActionResult> GetMyBusinesses()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var memberships = await _dbContext.OrganizationMemberships.IgnoreQueryFilters()
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var user = await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        var tenantIds = memberships.Select(m => m.TenantId).ToList();
        if (user != null && user.TenantId != Guid.Empty && !tenantIds.Contains(user.TenantId))
        {
            tenantIds.Add(user.TenantId);
        }

        var businesses = await _dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Category,
                t.City,
                t.LogoUrl,
                Role = user != null && user.TenantId == t.Id ? user.Role : "Staff"
            })
            .ToListAsync();

        return Ok(businesses);
    }

    [HttpPost("switch-business")]
    [Authorize]
    public async Task<IActionResult> SwitchBusiness([FromBody] SwitchBusinessRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { Message = "User not found." });

        var targetTenant = await _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == request.TenantId);
        if (targetTenant == null) return NotFound(new { Message = "Business not found." });

        // Generate new JWT scoped to the switched business
        var tokenResult = _tokenGenerator.GenerateAccessToken(user, targetTenant.Id);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _tokenGenerator.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiryUtc = DateTime.UtcNow.AddDays(7);

        return Ok(new AuthResponse(
            tokenResult.Token,
            tokenResult.ExpiryUtc,
            rawRefreshToken,
            refreshTokenExpiryUtc,
            user.Id,
            targetTenant.Id,
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

public record RegisterCustomerRequest(string Email, string Password, string FirstName, string LastName, string? Phone);
public record SwitchBusinessRequest(Guid TenantId);

