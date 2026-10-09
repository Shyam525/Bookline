using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Specification Section 63: DOUBLE BOOKING
/// Two customers attempt same slot.
/// Exactly one succeeds.
/// Other receives: SLOT_UNAVAILABLE.
/// Test: 2 concurrent, 10 concurrent, 50 concurrent.
/// </summary>
public class DoubleBookingConcurrencyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly DateTimeOffset _startUtc = DateTimeOffset.UtcNow.AddHours(2);

    [Fact]
    public async Task Section63_TwoConcurrent_ExactlyOneSucceeds_OtherReceivesSlotUnavailable()
    {
        // Arrange
        var holdService = new SlotHoldService(redis: null);
        var concurrency = 2;

        // Act
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => holdService.AcquireHoldAsync(_tenantId, _staffId, _startUtc, TimeSpan.FromMinutes(5)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successful = results.Count(r => r != null);
        var failed = results.Count(r => r == null);

        Assert.Equal(1, successful);
        Assert.Equal(1, failed);
    }

    [Fact]
    public async Task Section63_TenConcurrent_ExactlyOneSucceeds_NineReceiveSlotUnavailable()
    {
        // Arrange
        var holdService = new SlotHoldService(redis: null);
        var concurrency = 10;

        // Act
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => holdService.AcquireHoldAsync(_tenantId, _staffId, _startUtc, TimeSpan.FromMinutes(5)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successful = results.Count(r => r != null);
        var failed = results.Count(r => r == null);

        Assert.Equal(1, successful);
        Assert.Equal(9, failed);
    }

    [Fact]
    public async Task Section63_FiftyConcurrent_ExactlyOneSucceeds_FortyNineReceiveSlotUnavailable()
    {
        // Arrange
        var holdService = new SlotHoldService(redis: null);
        var concurrency = 50;

        // Act
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => holdService.AcquireHoldAsync(_tenantId, _staffId, _startUtc, TimeSpan.FromMinutes(5)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successful = results.Count(r => r != null);
        var failed = results.Count(r => r == null);

        Assert.Equal(1, successful);
        Assert.Equal(49, failed);
    }

    [Fact]
    public async Task Section63_ConcurrentHoldSlotCommandHandler_ThrowsSlotUnavailable()
    {
        // Arrange
        var holdService = new SlotHoldService(redis: null);
        var tenantContext = new TestTenantContext(_tenantId);

        var handler = new HoldSlotCommandHandler(holdService, tenantContext, null!);
        var command = new HoldSlotCommand(_staffId, Guid.NewGuid(), _startUtc);

        // Act: 10 concurrent requests to the handler
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                var result = await handler.Handle(command, CancellationToken.None);
                return (Success: true, Error: (string?)null);
            }
            catch (ValidationException ex)
            {
                return (Success: false, Error: ex.Message);
            }
        }).ToList();

        var outcomes = await Task.WhenAll(tasks);

        // Assert: Exactly one succeeds
        var successful = outcomes.Count(o => o.Success);
        var failed = outcomes.Where(o => !o.Success).ToList();

        Assert.Equal(1, successful);
        Assert.Equal(9, failed.Count);
        Assert.All(failed, f => Assert.Contains("SLOT_UNAVAILABLE", f.Error));
    }
}
