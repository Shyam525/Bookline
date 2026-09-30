namespace Bookline.Infrastructure.UnitTests;

using Xunit;
using Bookline.Infrastructure.Services;

public class SlotHoldServiceTests
{
    private readonly SlotHoldService _holdService = new(redis: null);

    [Fact]
    public async Task AcquireHold_WhenSlotIsFree_ShouldReturnHoldId()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddHours(2);

        var holdId = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));

        Assert.NotNull(holdId);
        Assert.NotEqual(Guid.Empty, holdId);
    }

    [Fact]
    public async Task AcquireHold_WhenSlotIsAlreadyHeld_ShouldReturnNull()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddHours(3);

        var firstHold = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));
        var secondHold = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));

        Assert.NotNull(firstHold);
        Assert.Null(secondHold);
    }

    [Fact]
    public async Task ValidateHold_WithMatchingHoldId_ShouldReturnTrue()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddHours(4);

        var holdId = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));
        var isValid = await _holdService.ValidateHoldAsync(tenantId, staffId, startUtc, holdId!.Value);

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateHold_WithInvalidHoldId_ShouldReturnFalse()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddHours(5);

        await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));
        var isValid = await _holdService.ValidateHoldAsync(tenantId, staffId, startUtc, Guid.NewGuid());

        Assert.False(isValid);
    }

    [Fact]
    public async Task ReleaseHold_ShouldFreeSlotForNewAcquire()
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddHours(6);

        var holdId = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));
        await _holdService.ReleaseHoldAsync(tenantId, staffId, startUtc, holdId!.Value);

        var newHoldId = await _holdService.AcquireHoldAsync(tenantId, staffId, startUtc, TimeSpan.FromMinutes(30));

        Assert.NotNull(newHoldId);
    }
}
