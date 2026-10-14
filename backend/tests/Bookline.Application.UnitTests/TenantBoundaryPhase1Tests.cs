using System.Security.Claims;
using Bookline.Api.Controllers;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
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

namespace Bookline.Application.UnitTests;

public class TenantBoundaryPhase1Tests
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly IConfiguration _config;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;

    public TenantBoundaryPhase1Tests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456!"},
            {"Jwt:Issuer", "BooklineTest"},
            {"Jwt:Audience", "BooklineTest"},
            {"Jwt:ExpiryMinutes", "60"}
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _tokenGenerator = new JwtTokenGenerator(_config);
        _passwordHasher = new PasswordHasher();
        _emailSender = new FakeEmailSender();
    }

    private BooklineDbContext CreateDbContext(string dbName, Guid? activeTenantId = null)
    {
        var tenantContext = new TenantContext();
        if (activeTenantId.HasValue)
        {
            tenantContext.SetTenantId(activeTenantId.Value);
        }

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    private static AuthController CreateAuthController(BooklineDbContext db, IPasswordHasher hasher, IJwtTokenGenerator tokenGen, IEmailSender emailSender, Guid currentUserId, string role = "Owner")
    {
        var controller = new AuthController(db, hasher, tokenGen, emailSender);
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim("sub", currentUserId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        return controller;
    }

    [Fact]
    public void OrganizationMembership_ShouldSupportIsActiveFlagAndMultipleRoles()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Role = "Manager",
            IsActive = true
        };

        // Assert default & properties
        Assert.True(membership.IsActive);
        Assert.Equal("Manager", membership.Role);

        // Act
        membership.IsActive = false;

        // Assert
        Assert.False(membership.IsActive);
    }

    [Fact]
    public async Task Customer_ShouldSupportPlatformUserIdLinkageAcrossMultipleTenants()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var platformUserId = Guid.NewGuid();

        using var db = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = platformUserId,
            Email = "customer@platform.com",
            FirstName = "Universal",
            LastName = "Customer",
            Role = "Customer",
            PasswordHash = "hash"
        };
        db.Users.Add(user);

        // Two independent customer records in different tenants linked to the same platform user
        var customerTenantA = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantA,
            UserId = platformUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Notes = "Prefers morning appointments"
        };

        var customerTenantB = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantB,
            UserId = platformUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Notes = "Allergic to certain hair dyes"
        };

        db.Customers.AddRange(customerTenantA, customerTenantB);
        await db.SaveChangesAsync();

        // Act: Query customers linked to the universal user
        var linkedCustomers = await db.Customers.IgnoreQueryFilters()
            .Where(c => c.UserId == platformUserId)
            .ToListAsync();

        // Assert
        Assert.Equal(2, linkedCustomers.Count);
        Assert.Contains(linkedCustomers, c => c.TenantId == _tenantA && (c.Notes ?? "").Contains("morning"));
        Assert.Contains(linkedCustomers, c => c.TenantId == _tenantB && (c.Notes ?? "").Contains("hair dyes"));
    }

    [Fact]
    public async Task SwitchBusiness_WhenUserIsActiveMember_ShouldReturn200AndIssueTenantScopedToken()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();

        using var db = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = userId,
            Email = "staff@salona.com",
            FirstName = "Jane",
            LastName = "Doe",
            Role = "Staff",
            TenantId = _tenantA,
            PasswordHash = "hash"
        };
        var targetTenant = new Tenant
        {
            Id = _tenantB,
            Name = "Salon Bravo",
            Slug = "salon-bravo",
            IsActive = true
        };
        var membership = new OrganizationMembership
        {
            UserId = userId,
            TenantId = _tenantB,
            Role = "Manager",
            IsActive = true
        };

        db.Users.Add(user);
        db.Tenants.Add(targetTenant);
        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender, userId, "Staff");

        // Act
        var result = await controller.SwitchBusiness(new SwitchBusinessRequest(_tenantB));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal(_tenantB, authResponse.TenantId);
        Assert.Equal("Manager", authResponse.Role);
        Assert.NotNull(authResponse.AccessToken);
    }

    [Fact]
    public async Task SwitchBusiness_WhenUserIsNotMember_ShouldReturn403Forbidden()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();

        using var db = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = userId,
            Email = "outsider@other.com",
            FirstName = "Eve",
            LastName = "Hacker",
            Role = "Staff",
            TenantId = _tenantA,
            PasswordHash = "hash"
        };
        var targetTenant = new Tenant
        {
            Id = _tenantB,
            Name = "Protected Business",
            Slug = "protected-biz",
            IsActive = true
        };

        db.Users.Add(user);
        db.Tenants.Add(targetTenant);
        // Note: NO membership in Tenant B!
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender, userId, "Staff");

        // Act
        var result = await controller.SwitchBusiness(new SwitchBusinessRequest(_tenantB));

        // Assert: MUST return 403 Forbidden
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task SwitchBusiness_WhenMembershipIsInactive_ShouldReturn403Forbidden()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();

        using var db = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = userId,
            Email = "formerstaff@biz.com",
            FirstName = "Bob",
            LastName = "Terminated",
            Role = "Staff",
            TenantId = _tenantA,
            PasswordHash = "hash"
        };
        var targetTenant = new Tenant
        {
            Id = _tenantB,
            Name = "Secure Salon",
            Slug = "secure-salon",
            IsActive = true
        };
        var revokedMembership = new OrganizationMembership
        {
            UserId = userId,
            TenantId = _tenantB,
            Role = "Staff",
            IsActive = false // Revoked membership
        };

        db.Users.Add(user);
        db.Tenants.Add(targetTenant);
        db.OrganizationMemberships.Add(revokedMembership);
        await db.SaveChangesAsync();

        var controller = CreateAuthController(db, _passwordHasher, _tokenGenerator, _emailSender, userId, "Staff");

        // Act
        var result = await controller.SwitchBusiness(new SwitchBusinessRequest(_tenantB));

        // Assert: Revoked membership MUST return 403 Forbidden
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task RegisterTenant_ShouldAutomaticallyCreateActiveOwnerMembership()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbContext(dbName);

        var controller = new AuthController(db, _passwordHasher, _tokenGenerator, _emailSender);
        var request = new RegisterTenantRequest(
            TenantName: "Apex Barbershop",
            TenantSlug: "apex-barbershop",
            OwnerEmail: "owner@apexbarber.com",
            Password: "SecurePassword123!",
            FirstName: "Marcus",
            LastName: "Vance"
        );

        // Act
        var result = await controller.RegisterTenant(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.NotEqual(Guid.Empty, authResponse.TenantId);
        Assert.Equal("Owner", authResponse.Role);

        // Verify that OrganizationMembership was created and active
        var membership = await db.OrganizationMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == authResponse.UserId && m.TenantId == authResponse.TenantId);

        Assert.NotNull(membership);
        Assert.True(membership.IsActive);
        Assert.Equal("Owner", membership.Role);
    }

    [Fact]
    public async Task CustomerBookingResolution_WhenCustomerHasUniversalUserId_ShouldProvisionTenantCustomerLinkedToUser()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var platformUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var tenantContext = new TestTenantContext(tenantId);
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var db = new BooklineDbContext(options, tenantContext);

        // Platform customer user
        var platformUser = new AppUser
        {
            Id = platformUserId,
            Email = "customer@global.com",
            FirstName = "Global",
            LastName = "Customer",
            Phone = "+19998887777",
            Role = "Customer",
            PasswordHash = "hash"
        };
        db.Users.Add(platformUser);

        // Tenant setup
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Local Spa", Slug = "local-spa", IsActive = true });
        db.Staff.Add(new Bookline.Domain.Entities.Staff { Id = staffId, TenantId = tenantId, TimeZoneId = "UTC" });
        db.Services.Add(new Service { Id = serviceId, TenantId = tenantId, Name = "Massage", DurationMinutes = 60, Price = 100 });
        await db.SaveChangesAsync();

        var slotHoldService = new SlotHoldService(null);
        var slotEngine = new SlotEngine();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var holdId = await slotHoldService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(60));
        Assert.NotNull(holdId);

        var handler = new CreateBookingCommandHandler(db, tenantContext, slotHoldService, slotEngine);

        // Act: Customer books passing their platform AppUser ID as CustomerId
        var command = new CreateBookingCommand(
            StaffId: staffId,
            ServiceId: serviceId,
            CustomerId: platformUserId, // AppUser.Id
            StartUtc: startUtc,
            HoldId: holdId.Value
        );

        var bookingResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, bookingResult.Id);

        // Verify that a tenant-specific Customer was provisioned and linked to platformUserId
        var localCustomer = await db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.UserId == platformUserId);

        Assert.NotNull(localCustomer);
        Assert.Equal("customer@global.com", localCustomer.Email);
        Assert.Equal("Global", localCustomer.FirstName);
        Assert.Equal("Customer", localCustomer.LastName);
        Assert.Equal(platformUserId, localCustomer.UserId);
    }
}

public class FakeEmailSender : IEmailSender
{
    public Task SendBookingConfirmationEmailAsync(Booking booking, Service service, Bookline.Domain.Entities.Staff staff, Customer customer, Tenant tenant, string rescheduleToken, string cancelToken, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
