using System.Security.Claims;
using Bookline.Api.Controllers;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Application.Tenants.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Bookline.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Application.UnitTests;

public class ProviderOnboardingPhase2Tests
{
    private (BooklineDbContext DbContext, TenantContext TenantContext) CreateDbContext(string dbName, Guid? tenantId = null)
    {
        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
        {
            tenantContext.SetTenantId(tenantId.Value);
        }

        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        return (context, tenantContext);
    }

    private static OnboardingController CreateController(ISender sender, ITeamAuthorizationService teamAuth, ITenantContext tenantContext, Guid callerId, string role = "Owner")
    {
        var controller = new OnboardingController(sender, teamAuth, tenantContext);
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, callerId.ToString()),
            new Claim("sub", callerId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        return controller;
    }

    [Fact]
    public void NewTenant_DefaultState_ShouldBeUnpublishedAndUncompleted()
    {
        // Section 5.11: New business must start unpublished and uncompleted until verified ready
        var tenant = new Tenant
        {
            Name = "New Aesthetic Clinic",
            Slug = "new-aesthetic"
        };

        Assert.False(tenant.IsPublished);
        Assert.False(tenant.IsOnboardingCompleted);
        Assert.Equal(1, tenant.OnboardingStep);
    }

    [Fact]
    public async Task SaveOnboardingStep_WhenBusinessNameIsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Initial Name", Slug = "initial-name" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act & Assert: Empty name on Step 2 must throw ValidationException
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(Step: 2, BusinessName: "   ")), CancellationToken.None));

        Assert.Contains("Business name is required", ex.Message);
    }

    [Fact]
    public async Task SaveOnboardingStep_WhenInvalidTimezone_ShouldThrowValidationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Valid Name", Slug = "valid-name" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act & Assert: Invalid IANA timezone on Step 6 must throw ValidationException
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(Step: 6, TimeZoneId: "Invalid/Fake_Zone")), CancellationToken.None));

        Assert.Contains("Invalid IANA timezone", ex.Message);
    }

    [Fact]
    public async Task SaveOnboardingStep_WhenInvalidWorkingHours_ShouldThrowValidationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Hours Clinic", Slug = "hours-clinic" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act & Assert: End time <= Start time must throw ValidationException
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
                Step: 10,
                StartTime: new TimeOnly(18, 0),
                EndTime: new TimeOnly(9, 0)
            )), CancellationToken.None));

        Assert.Contains("Operating closing time must be after opening time", ex.Message);
    }

    [Fact]
    public async Task SaveOnboardingStep_WhenInvalidBookingPolicies_ShouldThrowValidationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Policy Salon", Slug = "policy-salon" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act & Assert: Hold duration <= 0 must throw ValidationException
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
                Step: 11,
                HoldDurationMinutes: 0
            )), CancellationToken.None));

        Assert.Contains("Hold duration must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task PublishStorefront_WhenTenantIsSuspended_ShouldBlockPublicationWithStructuredMessage()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Suspended Business",
            Slug = "suspended-biz",
            VerificationStatus = VerificationStatus.Suspended, // Platform suspended
            IsPublished = false
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var handler = new OnboardingHandlers(db, context);

        // Act
        var result = await handler.Handle(new PublishStorefrontCommand(), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsPublished);
        Assert.Contains("suspended by platform administration", result.Message);
    }

    [Fact]
    public async Task OnboardingController_WhenCallerIsNotOwnerOrAdmin_ShouldReturnForbid()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var unauthorizedUserId = Guid.NewGuid();
        var outsider = new AppUser
        {
            Id = unauthorizedUserId,
            Email = "customer@outsider.com",
            Role = "Customer",
            TenantId = Guid.Empty,
            PasswordHash = "hash"
        };
        db.Users.Add(outsider);

        var targetTenant = new Tenant { Id = tenantId, Name = "Protected Org", Slug = "protected-org" };
        db.Tenants.Add(targetTenant);
        await db.SaveChangesAsync();

        var teamAuth = new TeamAuthorizationService(db);
        var mockSender = new MockSender();
        var controller = CreateController(mockSender, teamAuth, context, unauthorizedUserId, "Customer");

        // Act: Customer attempts to read onboarding status of the business
        var result = await controller.GetStatus(CancellationToken.None);

        // Assert: MUST return 403 Forbidden (ForbidResult)
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task OnboardingController_WhenMembershipIsInactive_ShouldReturnForbid()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var revokedUserId = Guid.NewGuid();
        var revokedUser = new AppUser
        {
            Id = revokedUserId,
            Email = "formerowner@org.com",
            Role = "Owner",
            TenantId = Guid.Empty,
            PasswordHash = "hash"
        };
        var targetTenant = new Tenant { Id = tenantId, Name = "Revoked Org", Slug = "revoked-org" };
        var revokedMembership = new OrganizationMembership
        {
            UserId = revokedUserId,
            TenantId = tenantId,
            Role = "Owner",
            IsActive = false // Deactivated membership!
        };

        db.Users.Add(revokedUser);
        db.Tenants.Add(targetTenant);
        db.OrganizationMemberships.Add(revokedMembership);
        await db.SaveChangesAsync();

        var teamAuth = new TeamAuthorizationService(db);
        var mockSender = new MockSender();
        var controller = CreateController(mockSender, teamAuth, context, revokedUserId, "Owner");

        // Act
        var result = await controller.GetStatus(CancellationToken.None);

        // Assert: Deactivated membership MUST be rejected with ForbidResult
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task OnboardingController_WhenCallerIsAuthorizedOwner_ShouldAllowAccess()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(dbName, tenantId);

        var ownerId = Guid.NewGuid();
        var owner = new AppUser
        {
            Id = ownerId,
            Email = "owner@validorg.com",
            Role = "Owner",
            TenantId = tenantId,
            PasswordHash = "hash"
        };
        var targetTenant = new Tenant { Id = tenantId, Name = "Valid Org", Slug = "valid-org", OnboardingStep = 2 };
        var activeMembership = new OrganizationMembership
        {
            UserId = ownerId,
            TenantId = tenantId,
            Role = "Owner",
            IsActive = true
        };

        db.Users.Add(owner);
        db.Tenants.Add(targetTenant);
        db.OrganizationMemberships.Add(activeMembership);
        await db.SaveChangesAsync();

        var teamAuth = new TeamAuthorizationService(db);
        var mockSender = new MockSender(new OnboardingStatusDto(
            tenantId, "Valid Org", "valid-org", "Salon", "Category", "Desc", null, null,
            "123 Main St", "City", "State", "12345", "Country", "UTC", "USD", 2, false, false,
            "Verified", "Approved", null, null, null, null, null, null, null, null, null, null,
            null, 5, 2, 30, 4, new List<int> { 1, 2 }
        ));

        var controller = CreateController(mockSender, teamAuth, context, ownerId, "Owner");

        // Act
        var result = await controller.GetStatus(CancellationToken.None);

        // Assert: 200 OK
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var statusDto = Assert.IsType<OnboardingStatusDto>(okResult.Value);
        Assert.Equal("Valid Org", statusDto.BusinessName);
    }
}

public class MockSender : ISender
{
    private readonly object? _response;

    public MockSender(object? response = null)
    {
        _response = response;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (_response is TResponse typedResponse)
        {
            return Task.FromResult(typedResponse);
        }
        return Task.FromResult(default(TResponse)!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_response);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
