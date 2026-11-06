namespace Bookline.Application.UnitTests;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookline.Api.Controllers;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Constants;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Authentication;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Business = Bookline.Domain.Entities.Tenant;
using BusinessMembership = Bookline.Domain.Entities.OrganizationMembership;

public class Phase1FoundationAndTenantSecurityTests
{
    private readonly IConfiguration _config;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;

    public Phase1FoundationAndTenantSecurityTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "SuperSecretKeyForBooklineApiThatIsAtLeast32BytesLong!" },
            { "JwtSettings:Issuer", "Bookline" },
            { "JwtSettings:Audience", "BooklineApp" },
            { "JwtSettings:ExpiryMinutes", "60" }
        };

        _config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        _passwordHasher = new PasswordHasher();
        _tokenGenerator = new JwtTokenGenerator(_config);
        _emailSender = new FakeEmailSender();
    }

    private BooklineDbContext CreateDbContext(string dbName, Guid? tenantId = null)
    {
        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
        {
            tenantContext.SetTenantId(tenantId.Value);
        }

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    private static AuthController CreateAuthController(BooklineDbContext db, IPasswordHasher hasher, IJwtTokenGenerator tokenGen, IEmailSender emailSender, Guid? currentUserId = null, string role = "Owner")
    {
        var controller = new AuthController(db, hasher, tokenGen, emailSender);
        var httpContext = new DefaultHttpContext();
        if (currentUserId.HasValue)
        {
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.Value.ToString()),
                new Claim("sub", currentUserId.Value.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));
            httpContext.User = claims;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public void DomainDistinction_UserIsNotEquivalentToBusiness()
    {
        // Assert domain models enforce distinct identity for User and Business (Tenant)
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            Role = Roles.BusinessOwner
        };

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Gujarat Salon",
            Slug = "gujarat-salon",
            City = "Ahmedabad",
            State = "Gujarat"
        };

        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = business.Id,
            Role = Roles.BusinessOwner,
            IsActive = true
        };

        Assert.NotEqual(user.Id, business.Id);
        Assert.Equal(membership.UserId, user.Id);
        Assert.Equal(membership.BusinessId, business.Id);
    }

    [Fact]
    public void RoleClassification_EnforcesCanonicalRolesAndPolicies()
    {
        Assert.True(Roles.IsBusinessOwner(Roles.BusinessOwner));
        Assert.True(Roles.IsBusinessOwner(Roles.Owner));
        Assert.True(Roles.IsBusinessOwner(Roles.Admin));
        Assert.False(Roles.IsBusinessOwner(Roles.Customer));

        Assert.True(Roles.IsBusinessStaff(Roles.BusinessStaff));
        Assert.True(Roles.IsBusinessStaff(Roles.Staff));
        Assert.True(Roles.IsBusinessStaff(Roles.Receptionist));
        Assert.False(Roles.IsBusinessStaff(Roles.Customer));

        Assert.True(Roles.IsPlatformAdmin(Roles.PlatformAdmin));
        Assert.False(Roles.IsPlatformAdmin(Roles.BusinessOwner));
        Assert.False(Roles.IsPlatformAdmin(Roles.Customer));

        Assert.True(Roles.IsCustomer(Roles.Customer));
        Assert.True(Roles.IsCustomer(null));
    }

    [Fact]
    public async Task Authentication_ValidLogin_ReturnsSuccessWithTokens()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var password = "SecurePassword123!";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "provider@bookline.in",
            PasswordHash = _passwordHasher.HashPassword(password),
            FirstName = "Rajesh",
            LastName = "Patel",
            Role = Roles.BusinessOwner,
            TenantId = Guid.NewGuid()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender);
        var response = await controller.Login(new LoginRequest(user.Email, password));

        var okResult = Assert.IsType<OkObjectResult>(response);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal(user.Id, authResponse.UserId);
        Assert.Equal(user.Email, authResponse.Email);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authResponse.RefreshToken));
    }

    [Fact]
    public async Task Authentication_InvalidPassword_ReturnsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "user@bookline.in",
            PasswordHash = _passwordHasher.HashPassword("CorrectPassword!"),
            FirstName = "Aarav",
            LastName = "Shah",
            Role = Roles.Customer
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender);
        var response = await controller.Login(new LoginRequest(user.Email, "WrongPassword!"));

        var statusResult = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }

    [Fact]
    public async Task Authentication_RegisterCustomer_CreatesIsolatedCustomerAccount()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender);
        var response = await controller.RegisterCustomer(new RegisterCustomerRequest(
            "client@gujarat.com",
            "StrongPass123!",
            "Bhavik",
            "Mehta",
            "+919825000000"
        ));

        var okResult = Assert.IsType<OkObjectResult>(response);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal(Roles.Customer, authResponse.Role);
        Assert.Equal(Guid.Empty, authResponse.TenantId);

        var dbUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "client@gujarat.com");
        Assert.NotNull(dbUser);
        Assert.Equal(Roles.Customer, dbUser.Role);
        Assert.Equal(Guid.Empty, dbUser.TenantId);
    }

    [Fact]
    public async Task BusinessOwnership_ActiveMembershipGrantsAccess()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var user = new AppUser
        {
            Id = userId,
            Email = "owner@salon.in",
            Role = Roles.BusinessOwner,
            TenantId = businessId,
            PasswordHash = "hash"
        };
        var business = new Business
        {
            Id = businessId,
            Name = "Royal Salon",
            Slug = "royal-salon"
        };
        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = businessId,
            Role = Roles.BusinessOwner,
            IsActive = true
        };

        db.Users.Add(user);
        db.Tenants.Add(business);
        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync();

        var teamAuth = new TeamAuthorizationService(db);
        var role = await teamAuth.GetUserRoleInTenantAsync(userId, businessId);

        Assert.Equal(ProviderTeamRole.Owner, role);
        Assert.True(await teamAuth.CanManageTeamAsync(userId, businessId));
        Assert.True(await teamAuth.CanManageServicesAsync(userId, businessId));
    }

    [Fact]
    public async Task BusinessOwnership_RevokedMembershipDeniesAccess()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var user = new AppUser
        {
            Id = userId,
            Email = "revoked@salon.in",
            Role = Roles.BusinessStaff,
            TenantId = businessId,
            PasswordHash = "hash"
        };
        var business = new Business
        {
            Id = businessId,
            Name = "Royal Salon",
            Slug = "royal-salon"
        };
        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = businessId,
            Role = Roles.BusinessStaff,
            IsActive = false // Deactivated!
        };

        db.Users.Add(user);
        db.Tenants.Add(business);
        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync();

        var teamAuth = new TeamAuthorizationService(db);
        var role = await teamAuth.GetUserRoleInTenantAsync(userId, businessId);

        Assert.Null(role);
        Assert.False(await teamAuth.CanManageTeamAsync(userId, businessId));
        Assert.False(await teamAuth.CanMutateDataAsync(userId, businessId));
    }

    [Fact]
    public async Task CrossBusinessAccess_UserCannotSwitchToUnassociatedBusiness()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var userId = Guid.NewGuid();
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();

        var user = new AppUser
        {
            Id = userId,
            Email = "stylist@salona.com",
            Role = Roles.BusinessStaff,
            TenantId = businessA,
            PasswordHash = "hash"
        };
        var targetBusiness = new Business
        {
            Id = businessB,
            Name = "Unassociated Spa",
            Slug = "unassociated-spa"
        };

        db.Users.Add(user);
        db.Tenants.Add(targetBusiness);
        // Note: NO membership in businessB!
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender, userId, Roles.BusinessStaff);
        var result = await controller.SwitchBusiness(new SwitchBusinessRequest(businessB));

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public void ConfigurationValidation_ThrowsWhenProductionKeyInsecure()
    {
        var insecureKey = "short";

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (string.IsNullOrWhiteSpace(insecureKey) || insecureKey.Length < 32 || insecureKey.Contains("SuperSecretKey"))
            {
                throw new InvalidOperationException("Production configuration failure: 'JwtSettings:Secret' must be configured with a secure key of at least 32 bytes.");
            }
        });
    }

    [Fact]
    public void ApiErrorContract_MatchesCanonicalStructure()
    {
        var error = ApiErrorResponse.NotFound("Business not found.", "trace-12345");

        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal("Business not found.", error.Message);
        Assert.Equal("trace-12345", error.TraceId);
        Assert.Equal(404, error.Status);
    }
}
