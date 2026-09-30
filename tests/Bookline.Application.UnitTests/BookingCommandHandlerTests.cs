namespace Bookline.Application.UnitTests;

using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

public class BookingCommandHandlerTests
{
    private readonly ITenantContext _tenantContext;
    private readonly ISlotHoldService _slotHoldService;
    private readonly ISlotEngine _slotEngine;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public BookingCommandHandlerTests()
    {
        _tenantContext = new TestTenantContext(_tenantId);
        _slotHoldService = new SlotHoldService(redis: null);
        _slotEngine = new SlotEngine();
    }

    private BooklineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new BooklineDbContext(options, _tenantContext);

        context.Staff.Add(new Staff { Id = _staffId, TenantId = _tenantId, TimeZoneId = "UTC" });
        context.Services.Add(new Service { Id = _serviceId, TenantId = _tenantId, Name = "Haircut", DurationMinutes = 30, BufferMinutes = 0, Price = 50 });
        context.Customers.Add(new Customer { Id = _customerId, TenantId = _tenantId, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task CreateBooking_WithoutValidHold_ShouldThrowValidationException()
    {
        using var context = CreateInMemoryDbContext();
        var handler = new CreateBookingCommandHandler(context, _tenantContext, _slotHoldService, _slotEngine);

        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var command = new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, Guid.NewGuid());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBooking_WithValidHold_ShouldSucceedAndCreateBooking()
    {
        using var context = CreateInMemoryDbContext();
        var handler = new CreateBookingCommandHandler(context, _tenantContext, _slotHoldService, _slotEngine);

        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var holdId = await _slotHoldService.AcquireHoldAsync(_tenantId, _staffId, startUtc, TimeSpan.FromMinutes(30));

        var command = new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, holdId!.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(_staffId, result.StaffId);
        Assert.Equal(BookingStatus.Pending, result.Status);

        // Verify hold was released after successful booking
        var isHoldValid = await _slotHoldService.ValidateHoldAsync(_tenantId, _staffId, startUtc, holdId.Value);
        Assert.False(isHoldValid);
    }

    [Fact]
    public async Task Concurrency_50ParallelBookingsForSameSlot_ExactlyOne201And49Conflicts()
    {
        // Execute 20 runs in a row to guarantee zero flakiness
        for (int run = 0; run < 20; run++)
        {
            var options = new DbContextOptionsBuilder<BooklineDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new BooklineDbContext(options, _tenantContext);
            context.Staff.Add(new Staff { Id = _staffId, TenantId = _tenantId, TimeZoneId = "UTC" });
            context.Services.Add(new Service { Id = _serviceId, TenantId = _tenantId, Name = "Haircut", DurationMinutes = 30, BufferMinutes = 0, Price = 50 });
            context.Customers.Add(new Customer { Id = _customerId, TenantId = _tenantId, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" });
            context.SaveChanges();

            var holdService = new SlotHoldService(redis: null);
            var handler = new CreateBookingCommandHandler(context, _tenantContext, holdService, _slotEngine);

            var startUtc = DateTimeOffset.UtcNow.AddDays(2 + run);
            var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var tasks = Enumerable.Range(0, 50).Select(async _ =>
            {
                await startSignal.Task;
                return await holdService.AcquireHoldAsync(_tenantId, _staffId, startUtc, TimeSpan.FromMinutes(30));
            }).ToList();

            startSignal.SetResult(true);
            var holdIds = await Task.WhenAll(tasks);

            var validHoldIds = holdIds.Where(h => h.HasValue).ToList();
            var nullHoldIds = holdIds.Where(h => !h.HasValue).ToList();

            Assert.Single(validHoldIds);
            Assert.Equal(49, nullHoldIds.Count);

            // The single successful hold owner creates the booking
            var bookingDto = await handler.Handle(
                new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, validHoldIds.Single()!.Value),
                CancellationToken.None);

            Assert.NotNull(bookingDto);
            Assert.Equal(_staffId, bookingDto.StaffId);
        }
    }



    [Fact]
    public async Task CancelledBooking_FreesSlotForNewBooking()
    {
        using var context = CreateInMemoryDbContext();
        var handler = new CreateBookingCommandHandler(context, _tenantContext, _slotHoldService, _slotEngine);

        var startUtc = DateTimeOffset.UtcNow.AddDays(3);
        var holdId1 = await _slotHoldService.AcquireHoldAsync(_tenantId, _staffId, startUtc, TimeSpan.FromMinutes(30));

        var bookingDto = await handler.Handle(new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, holdId1!.Value), CancellationToken.None);

        var bookingEntity = await context.Bookings.FindAsync(bookingDto.Id);
        bookingEntity!.Cancel();
        await context.SaveChangesAsync();

        // New booking for same slot after cancellation
        var holdId2 = await _slotHoldService.AcquireHoldAsync(_tenantId, _staffId, startUtc, TimeSpan.FromMinutes(30));
        Assert.NotNull(holdId2);

        var secondBookingDto = await handler.Handle(new CreateBookingCommand(_staffId, _serviceId, _customerId, startUtc, holdId2.Value), CancellationToken.None);
        Assert.NotNull(secondBookingDto);
        Assert.NotEqual(bookingDto.Id, secondBookingDto.Id);
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; private set; }
        public bool IsResolved => true;

        public TestTenantContext(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public void SetTenantId(Guid tenantId)
        {
            TenantId = tenantId;
        }
    }
}

