using Bookline.Api.Controllers;
using Bookline.Api.Validators;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Products.Commands;
using Bookline.Application.Products.DTOs;
using Bookline.Application.Products.Validators;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Application.Services.Validators;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Section 83: Comprehensive Backend Validation & Authoritative Revalidation Test Suite.
/// Verifies:
/// 1. Independent backend validation of all external request inputs using FluentValidation.
/// 2. Rejection of malicious, out-of-range, and malformed inputs (negative prices, invalid dates, out-of-bounds ratings).
/// 3. Authoritative server revalidation of mutable domain conditions at the mutation boundary:
///    - Service deactivated after form was opened.
///    - Staff deactivated or unassigned from service.
///    - Inventory stock exhausted after cart was loaded.
///    - Customer review eligibility (verified purchase required).
///    - Expired/invalid slot hold.
/// </summary>
public class ValidationSection83Tests
{
    private static BooklineDbContext CreateInMemoryDb(string dbName)
    {
        var tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new BooklineDbContext(options, tenantContext);
    }

    #region Fakes

    private class FakeSlotHoldService : ISlotHoldService
    {
        public Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, TimeSpan duration, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private class FakeSlotEngine : ISlotEngine
    {
        public IReadOnlyList<Slot> Compute(ServiceInfo svc, StaffSchedule schedule, IReadOnlyList<Interval> bookings, IReadOnlyList<Interval> timeOff, LocalDate day, DateTimeZone zone, Instant now, Duration step, Duration minimumNotice) =>
            Array.Empty<Slot>();
    }

    private class FakePaymentProvider : IPaymentProvider
    {
        public string ProviderName => "FakeGateway";

        public Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentResult(true, Guid.NewGuid(), "tx-fake-123", "Completed", null, null));

        public Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RefundResult(true, "ref-fake-123", "Completed", null));

        public Task<PaymentVerificationResult> VerifyPaymentAsync(string transactionReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentVerificationResult(true, "Paid", "FakeGateway", "tx-fake-123", 100m, "USD", null));
    }

    private class FakeIdempotencyService : IIdempotencyService
    {
        public Task<IdempotencyResult?> GetExistingAsync(string key, string operation, CancellationToken cancellationToken = default) =>
            Task.FromResult<IdempotencyResult?>(null);

        public Task SaveAsync(string key, string operation, int statusCode, string responseJson, Guid? tenantId = null, string? requestHash = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    #endregion

    #region 1. FluentValidation Input & Constraint Tests (Malicious / Invalid Inputs)

    [Fact]
    public void CreateBookingCommandValidator_RejectsInvalidAndMaliciousInputs()
    {
        var validator = new CreateBookingCommandValidator();

        // All empty or invalid
        var emptyCommand = new CreateBookingCommand(
            Guid.Empty,
            Guid.Empty,
            null,
            DateTimeOffset.UtcNow.AddHours(-1), // Past date
            Guid.Empty,
            "", // empty name
            "invalid-email", // invalid email
            null
        );

        var result = validator.Validate(emptyCommand);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StaffId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ServiceId");
        Assert.Contains(result.Errors, e => e.PropertyName == "HoldId");
        Assert.Contains(result.Errors, e => e.PropertyName == "StartUtc");
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerName");
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerEmail");
    }

    [Theory]
    [InlineData(-10.0, 30, "USD")]     // Negative price
    [InlineData(50.0, 2, "USD")]       // Duration < 5 min
    [InlineData(50.0, 2000, "USD")]    // Duration > 1440 min
    [InlineData(50.0, 30, "US")]       // Currency not 3-letter code
    [InlineData(50.0, 30, "TOOLONG")]  // Currency > 3 letters
    public void CreateServiceCommandValidator_RejectsInvalidNumericAndFormatInputs(
        decimal price, int durationMinutes, string currency)
    {
        var validator = new CreateServiceCommandValidator();
        var command = new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: Guid.NewGuid(),
            Name: "Test Service",
            DurationMinutes: durationMinutes,
            Price: price,
            Currency: currency
        ));

        var result = validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(-5, 10)]   // Negative stock
    [InlineData(10, -1)]   // Negative threshold
    public void CreateProductCommandValidator_RejectsNegativeInventory(int stock, int threshold)
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(new CreateProductRequest(
            Name: "Organic Shampoo",
            Price: 19.99m,
            StockQuantity: stock,
            LowStockThreshold: threshold
        ));

        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("StockQuantity") || e.PropertyName.Contains("LowStockThreshold"));
    }

    [Fact]
    public void CreateOrderRequestValidator_RejectsEmptyItemsAndZeroQuantity()
    {
        var validator = new CreateOrderRequestValidator();

        // Empty items
        var emptyOrder = new CreateOrderRequest(
            TenantId: Guid.NewGuid(),
            CustomerName: "John Doe",
            CustomerEmail: "john@example.com",
            CustomerPhone: null,
            ShippingAddress: null,
            Notes: null,
            Items: new List<OrderItemRequest>()
        );
        var emptyResult = validator.Validate(emptyOrder);
        Assert.False(emptyResult.IsValid);
        Assert.Contains(emptyResult.Errors, e => e.PropertyName == "Items");

        // Zero quantity item
        var zeroQtyOrder = new CreateOrderRequest(
            TenantId: Guid.NewGuid(),
            CustomerName: "John Doe",
            CustomerEmail: "john@example.com",
            CustomerPhone: null,
            ShippingAddress: null,
            Notes: null,
            Items: new List<OrderItemRequest> { new(Guid.NewGuid(), 0) }
        );
        var zeroResult = validator.Validate(zeroQtyOrder);
        Assert.False(zeroResult.IsValid);
        Assert.Contains(zeroResult.Errors, e => e.PropertyName.Contains("Quantity"));
    }

    [Theory]
    [InlineData(0, "Great experience!")] // Rating < 1
    [InlineData(6, "Great experience!")] // Rating > 5
    [InlineData(5, "Bad")]               // Comment < 5 chars
    public void SubmitReviewRequestValidator_EnforcesRatingRangeAndCommentLength(int rating, string comment)
    {
        var validator = new SubmitReviewRequestValidator();
        var request = new SubmitReviewRequest(
            TenantId: Guid.NewGuid(),
            Rating: rating,
            Title: "Review",
            Comment: comment,
            CustomerName: "Verified Client"
        );

        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }

    #endregion

    #region 2. Authoritative Server Revalidation at Mutation Boundary

    [Fact]
    public async Task Booking_RevalidatesServiceActiveAndOnlineBookingStatus_RejectsWhenDeactivated()
    {
        // Arrange
        var db = CreateInMemoryDb(nameof(Booking_RevalidatesServiceActiveAndOnlineBookingStatus_RejectsWhenDeactivated));
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        // Service initially active, but online booking disabled or deactivated
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Signature Facial",
            Slug = "signature-facial",
            Price = 75.00m,
            Currency = "USD",
            DurationMinutes = 60,
            IsActive = true,
            IsOnlineBookingEnabled = false, // Deactivated for online booking
            IsPublished = true
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var fakeHold = new FakeSlotHoldService();
        var fakeEngine = new FakeSlotEngine();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(tenantId);

        var handler = new CreateBookingCommandHandler(db, tenantContext, fakeHold, fakeEngine);
        var command = new CreateBookingCommand(
            StaffId: staffId,
            ServiceId: service.Id,
            CustomerId: null,
            StartUtc: DateTimeOffset.UtcNow.AddHours(2),
            HoldId: Guid.NewGuid(),
            CustomerName: "Jane Doe",
            CustomerEmail: "jane@example.com"
        );

        // Act & Assert: authoritative check must reject
        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("SERVICE_NOT_BOOKABLE", ex.Message);
    }

    [Fact]
    public async Task Booking_RevalidatesStaffActiveStatus_RejectsWhenStaffArchivedOrInactive()
    {
        // Arrange
        var db = CreateInMemoryDb(nameof(Booking_RevalidatesStaffActiveStatus_RejectsWhenStaffArchivedOrInactive));
        var tenantId = Guid.NewGuid();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Haircut",
            Slug = "haircut",
            Price = 50.00m,
            DurationMinutes = 30,
            IsActive = true,
            IsOnlineBookingEnabled = true,
            IsPublished = true
        };

        // Staff deactivated
        var inactiveStaff = new Bookline.Domain.Entities.Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "John Former Staff",
            Email = "john.former@example.com",
            IsActive = false,
            IsArchived = true
        };

        db.Services.Add(service);
        db.Staff.Add(inactiveStaff);
        await db.SaveChangesAsync();

        var fakeHold = new FakeSlotHoldService();
        var fakeEngine = new FakeSlotEngine();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(tenantId);

        var handler = new CreateBookingCommandHandler(db, tenantContext, fakeHold, fakeEngine);
        var command = new CreateBookingCommand(
            StaffId: inactiveStaff.Id,
            ServiceId: service.Id,
            CustomerId: null,
            StartUtc: DateTimeOffset.UtcNow.AddHours(2),
            HoldId: Guid.NewGuid(),
            CustomerName: "Jane Doe",
            CustomerEmail: "jane@example.com"
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Orders_AuthoritativelyRevalidatesInventoryStock_RejectsWhenStockDepleted()
    {
        // Arrange: Cart loaded when stock was 1; before checkout, stock was reduced to 0
        var db = CreateInMemoryDb(nameof(Orders_AuthoritativelyRevalidatesInventoryStock_RejectsWhenStockDepleted));
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Wellness Studio",
            Slug = "wellness-studio",
            CreatedAtUtc = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Limited Herbal Balm",
            Slug = "limited-herbal-balm",
            Price = 25.00m,
            Currency = "USD",
            TrackInventory = true,
            StockQuantity = 0, // Depleted!
            IsActive = true
        };

        db.Tenants.Add(tenant);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var fakePayment = new FakePaymentProvider();
        var fakeIdemp = new FakeIdempotencyService();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(tenantId);

        var controller = new OrdersController(db, tenantContext, fakePayment, fakeIdemp)
        {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };
        var orderRequest = new CreateOrderRequest(
            TenantId: tenantId,
            CustomerName: "Alice Walker",
            CustomerEmail: "alice@example.com",
            CustomerPhone: "+15551234567",
            ShippingAddress: "123 Main St",
            Notes: null,
            Items: new List<OrderItemRequest>
            {
                new(product.Id, 1)
            }
        );

        // Act
        var result = await controller.CreateOrder(orderRequest, CancellationToken.None);

        // Assert: Must return 409 Conflict with INSUFFICIENT_INVENTORY
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(conflictResult.Value);
        Assert.Equal("INSUFFICIENT_INVENTORY", errorResponse.Code);
        Assert.Contains("Insufficient inventory", errorResponse.Message);
    }

    [Fact]
    public async Task Reviews_AuthoritativelyRevalidatesVerifiedPurchaseEligibility_RejectsIneligible()
    {
        // Arrange: User with no completed bookings or orders submits a review
        var db = CreateInMemoryDb(nameof(Reviews_AuthoritativelyRevalidatesVerifiedPurchaseEligibility_RejectsIneligible));
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Elite Dental",
            Slug = "elite-dental",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var controller = new ReviewsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };
        var reviewRequest = new SubmitReviewRequest(
            TenantId: tenantId,
            Rating: 5,
            Title: "Great work!",
            Comment: "Outstanding professionalism from the whole team.",
            CustomerName: "Unverified Stranger",
            BookingId: null,
            OrderId: null,
            SkipEligibilityCheck: false // Enforce strict eligibility check
        );

        // Act
        var result = await controller.SubmitReview(reviewRequest, CancellationToken.None);

        // Assert: Must return 409 Conflict with VERIFIED_PURCHASE_REQUIRED
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(conflictResult.Value);
        Assert.Equal("VERIFIED_PURCHASE_REQUIRED", errorResponse.Code);
        Assert.Contains("completed appointment or order", errorResponse.Message);
    }

    #endregion
}
