namespace Bookline.Application.UnitTests;

using Bookline.Api.Controllers;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Domain.Exceptions;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using System.Security.Claims;
using Xunit;

public class TelemetryAndFunnelIntegrityTests
{
    private readonly ITenantContext _tenantContext;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public TelemetryAndFunnelIntegrityTests()
    {
        _tenantContext = new TestTenantContext(_tenantId);
    }

    private BooklineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new BooklineDbContext(options, _tenantContext);

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Integrity Test Salon",
            Slug = "integrity-salon",
            City = "Ahmedabad",
            IsActive = true,
            IsPublished = true,
            IsDemo = true,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved
        };

        context.Tenants.Add(tenant);
        context.Staff.Add(new Staff { Id = _staffId, TenantId = _tenantId, TimeZoneId = "UTC", IsActive = true });
        context.Services.Add(new Service { Id = _serviceId, TenantId = _tenantId, Name = "Haircut", DurationMinutes = 30, BufferMinutes = 0, Price = 50, IsActive = true, IsPublished = true, IsOnlineBookingEnabled = true });
        context.Customers.Add(new Customer { Id = _customerId, TenantId = _tenantId, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task UpdateBookingStatus_Complete_SetsCompletedAtUtcFromClock()
    {
        using var context = CreateInMemoryDbContext();
        var fixedInstant = Instant.FromUtc(2026, 11, 10, 10, 30, 0);
        var fakeClock = new TestClock(fixedInstant);

        var booking = new Booking(_tenantId, _staffId, _serviceId, _customerId,
            fixedInstant.ToDateTimeOffset().AddDays(1),
            fixedInstant.ToDateTimeOffset().AddDays(1).AddMinutes(30));
        booking.Confirm();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var handler = new UpdateBookingStatusCommandHandler(context, fakeClock);
        var command = new UpdateBookingStatusCommand(booking.Id, "completed");

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(BookingStatus.Completed, updated.Status);
        Assert.Equal(fixedInstant.ToDateTimeOffset(), updated.CompletedAtUtc);
    }

    [Fact]
    public async Task UpdateBookingStatus_Complete_RepeatAttempt_ThrowsInvalidStatusTransitionException()
    {
        using var context = CreateInMemoryDbContext();
        var fixedInstant = Instant.FromUtc(2026, 11, 10, 10, 30, 0);
        var fakeClock = new TestClock(fixedInstant);

        var booking = new Booking(_tenantId, _staffId, _serviceId, _customerId,
            fixedInstant.ToDateTimeOffset().AddDays(1),
            fixedInstant.ToDateTimeOffset().AddDays(1).AddMinutes(30));
        booking.Confirm();
        booking.Complete(fixedInstant.ToDateTimeOffset());
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var handler = new UpdateBookingStatusCommandHandler(context, fakeClock);
        var command = new UpdateBookingStatusCommand(booking.Id, "completed");

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(fixedInstant.ToDateTimeOffset(), booking.CompletedAtUtc);
    }

    [Fact]
    public async Task RecordStorefrontFunnel_RejectClientSubmissionOfHoldCreated()
    {
        using var context = CreateInMemoryDbContext();
        var env = new TestWebHostEnvironment { EnvironmentName = "Production" };
        var controller = new TelemetryController(context, _tenantContext, NullLogger<TelemetryController>.Instance, env);

        var request = new RecordStorefrontFunnelRequest(
            Step: "HoldCreated",
            Slug: "integrity-salon",
            VisitorSessionId: "session-12345678"
        );

        var result = await controller.RecordStorefrontFunnel(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RecordStorefrontFunnel_RejectClientSubmissionOfBookingConfirmed()
    {
        using var context = CreateInMemoryDbContext();
        var env = new TestWebHostEnvironment { EnvironmentName = "Production" };
        var controller = new TelemetryController(context, _tenantContext, NullLogger<TelemetryController>.Instance, env);

        var request = new RecordStorefrontFunnelRequest(
            Step: "BookingConfirmed",
            Slug: "integrity-salon",
            VisitorSessionId: "session-12345678"
        );

        var result = await controller.RecordStorefrontFunnel(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Theory]
    [InlineData("short")] // < 8 characters
    [InlineData("invalid spaces in session id")] // spaces
    [InlineData("invalid<script>alert(1)</script>")] // XSS / symbols
    public async Task RecordStorefrontFunnel_RejectsInvalidVisitorSessionId(string invalidSessionId)
    {
        using var context = CreateInMemoryDbContext();
        var env = new TestWebHostEnvironment { EnvironmentName = "Production" };
        var controller = new TelemetryController(context, _tenantContext, NullLogger<TelemetryController>.Instance, env);

        var request = new RecordStorefrontFunnelRequest(
            Step: "Visitor",
            Slug: "integrity-salon",
            VisitorSessionId: invalidSessionId
        );

        var result = await controller.RecordStorefrontFunnel(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RecordStorefrontFunnel_RejectsUnknownTenantSlug()
    {
        using var context = CreateInMemoryDbContext();
        var env = new TestWebHostEnvironment { EnvironmentName = "Production" };
        var controller = new TelemetryController(context, _tenantContext, NullLogger<TelemetryController>.Instance, env);

        var request = new RecordStorefrontFunnelRequest(
            Step: "Visitor",
            Slug: "non-existent-salon-xyz",
            VisitorSessionId: "valid-session-12345678"
        );

        var result = await controller.RecordStorefrontFunnel(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task RecordStorefrontFunnel_IgnoresSpoofedInternalPreviewHeaderInProductionForAnonymousUser()
    {
        using var context = CreateInMemoryDbContext();
        var env = new TestWebHostEnvironment { EnvironmentName = "Production" };
        var controller = new TelemetryController(context, _tenantContext, NullLogger<TelemetryController>.Instance, env);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Internal-Preview"] = "true";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var request = new RecordStorefrontFunnelRequest(
            Step: "Visitor",
            Slug: "integrity-salon",
            VisitorSessionId: "session-preview-test-123"
        );

        var result = await controller.RecordStorefrontFunnel(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var recordedEvent = await context.StorefrontFunnelEvents
            .FirstOrDefaultAsync(f => f.VisitorSessionId == "session-preview-test-123");
        Assert.NotNull(recordedEvent);
        Assert.False(recordedEvent.IsInternalPreview); // Must NOT trust spoofed header in production
    }

    [Fact]
    public async Task HoldSlotCommandHandler_AuthoritativelyWritesHoldCreatedFunnelEvent()
    {
        using var context = CreateInMemoryDbContext();
        var slotHoldService = new SlotHoldService(redis: null);
        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var clock = new TestClock(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        var handler = new HoldSlotCommandHandler(slotHoldService, _tenantContext, context, clock);
        var command = new HoldSlotCommand(_staffId, _serviceId, startUtc);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        var funnelEvent = await context.StorefrontFunnelEvents
            .FirstOrDefaultAsync(f => f.RelatedHoldId == result.HoldId && f.Step == "HoldCreated");

        Assert.NotNull(funnelEvent);
        Assert.Equal(_tenantId, funnelEvent.TenantId);
        Assert.Equal("HoldCreated", funnelEvent.Step);
        Assert.False(funnelEvent.IsAutomatedOrBot);
    }

    [Fact]
    public async Task CreateBookingCommandHandler_AuthoritativelyWritesBookingConfirmedFunnelEvent()
    {
        using var context = CreateInMemoryDbContext();
        var slotHoldService = new SlotHoldService(redis: null);
        var slotEngine = new SlotEngine();
        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var clock = new TestClock(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        // Place hold first
        var holdDuration = TimeSpan.FromMinutes(5);
        var holdId = await slotHoldService.AcquireHoldAsync(_tenantId, _staffId, startUtc, holdDuration, CancellationToken.None);
        Assert.NotNull(holdId);

        var handler = new CreateBookingCommandHandler(context, _tenantContext, slotHoldService, slotEngine, clock: clock);
        var command = new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, holdId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        var funnelEvent = await context.StorefrontFunnelEvents
            .FirstOrDefaultAsync(f => f.RelatedBookingId == result.Id && f.Step == "BookingConfirmed");

        Assert.NotNull(funnelEvent);
        Assert.Equal(_tenantId, funnelEvent.TenantId);
        Assert.Equal("BookingConfirmed", funnelEvent.Step);
        Assert.Equal(holdId.Value, funnelEvent.RelatedHoldId);
    }
}

internal class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "Bookline.Api";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Development";
}

internal class TestClock : IClock
{
    private readonly Instant _instant;
    public TestClock(Instant instant) => _instant = instant;
    public Instant GetCurrentInstant() => _instant;
}
