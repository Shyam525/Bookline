using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Products.Commands;
using Bookline.Application.Products.DTOs;
using Bookline.Application.Products.Handlers;
using Bookline.Application.Products.Queries;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.UnitTests;

public class ProductManagementSection26Tests
{
    private (BooklineDbContext Db, TenantContext TenantCtx, ProductHandlers Handler) CreateTestContext(Guid? tenantId = null)
    {
        var activeTenantId = tenantId ?? Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.SetTenantId(activeTenantId);

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProductSection26Db_{Guid.NewGuid()}")
            .Options;

        var db = new BooklineDbContext(options, tenantContext);
        var imageService = new ProductImageService();
        var handler = new ProductHandlers(db, tenantContext, imageService);

        return (db, tenantContext, handler);
    }

    [Fact]
    public async Task CreateProduct_WithValidParameters_SuccessfullyPersistsProductAndGeneratesSlug()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Haircare",
            Slug = "haircare",
            IsActive = true
        };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateProductRequest(
            Name: "Keratin Restorative Treatment Mask",
            Description: "Intensive protein restructuring salon formula.",
            Price: 45.00m,
            Currency: "USD",
            Sku: "BL-KER-001",
            CategoryId: category.Id,
            Images: new List<ProductImageInput>
            {
                new("https://images.unsplash.com/photo-1556228720-195a672e8a03?w=600", "Front view", 0, true),
                new("https://images.unsplash.com/photo-1608248597359-24755f190696?w=600", "Back ingredients", 1, false)
            },
            TrackInventory: true,
            StockQuantity: 25,
            LowStockThreshold: 5,
            IsPurchasableOnline: true,
            IsPublished: true
        );

        var result = await handler.Handle(new CreateProductCommand(request), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Keratin Restorative Treatment Mask", result.Name);
        Assert.Equal("keratin-restorative-treatment-mask", result.Slug);
        Assert.Equal(45.00m, result.Price);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("BL-KER-001", result.Sku);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("Haircare", result.CategoryName);
        Assert.Equal(25, result.StockQuantity);
        Assert.Equal(25, result.AvailableQuantity);
        Assert.Equal(0, result.ReservedQuantity);
        Assert.True(result.IsInStock);
        Assert.False(result.IsLowStock);
        Assert.True(result.IsActive);
        Assert.True(result.IsPurchasableOnline);
        Assert.True(result.IsPublished);
        Assert.Equal("ActiveInStock", result.CommercialStatus);
        Assert.Equal("InStock", result.InventoryStatus);
        Assert.Equal(2, result.Images.Count);
        Assert.True(result.Images[0].IsPrimary);
        Assert.Equal(result.Images[0].Url, result.ImageUrl);
    }

    [Theory]
    [InlineData("", 50, "USD")]      // Empty name
    [InlineData("Valid", -10, "USD")] // Negative price
    [InlineData("Valid", 50, "")]     // Empty currency
    [InlineData("Valid", 50, "US")]   // Invalid currency length (not 3 chars)
    [InlineData("Valid", 50, "USDD")] // Invalid currency length (> 3 chars)
    public async Task CreateProduct_InvalidParameters_ThrowsValidationException(
        string name, decimal price, string currency)
    {
        var (db, _, handler) = CreateTestContext();

        var request = new CreateProductRequest(
            Name: name,
            Price: price,
            Currency: currency,
            StockQuantity: 10
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateProductCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProduct_NegativeInventoryStock_ThrowsValidationException()
    {
        var (db, _, handler) = CreateTestContext();

        var request = new CreateProductRequest(
            Name: "Test Product",
            Price: 20.00m,
            Currency: "USD",
            StockQuantity: -5
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateProductCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProduct_DuplicateSkuInSameBusiness_ThrowsValidationException()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        var existing = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Existing Product",
            Sku = "SKU-DUPLICATE-01",
            Price = 25.00m,
            Currency = "USD",
            IsActive = true
        };
        db.Products.Add(existing);
        await db.SaveChangesAsync();

        var request = new CreateProductRequest(
            Name: "New Product",
            Price: 30.00m,
            Currency: "USD",
            Sku: "sku-duplicate-01" // Case-insensitive collision
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateProductCommand(request), CancellationToken.None));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateProduct_SameSkuInDifferentBusinesses_IsAllowed()
    {
        var businessAId = Guid.NewGuid();
        var businessBId = Guid.NewGuid();

        var (dbA, _, handlerA) = CreateTestContext(businessAId);

        var existingA = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = businessAId,
            Name = "Business A Product",
            Sku = "SHARED-SKU-100",
            Price = 25.00m,
            Currency = "USD",
            IsActive = true
        };
        dbA.Products.Add(existingA);
        await dbA.SaveChangesAsync();

        // Switch to Business B
        var tenantCtxB = new TenantContext();
        tenantCtxB.SetTenantId(businessBId);
        var handlerB = new ProductHandlers(dbA, tenantCtxB, new ProductImageService());

        var requestB = new CreateProductRequest(
            Name: "Business B Product",
            Price: 40.00m,
            Currency: "USD",
            Sku: "SHARED-SKU-100"
        );

        var resultB = await handlerB.Handle(new CreateProductCommand(requestB), CancellationToken.None);

        Assert.NotNull(resultB);
        Assert.Equal("SHARED-SKU-100", resultB.Sku);
        Assert.Equal(businessBId, resultB.BusinessId);
    }

    [Fact]
    public async Task BusinessOwnership_TenantCannotAccessOrMutateOtherTenantsProducts()
    {
        var businessAId = Guid.NewGuid();
        var businessBId = Guid.NewGuid();

        var (db, tenantCtxA, handlerA) = CreateTestContext(businessAId);

        var prodA = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = businessAId,
            Name = "A Secret Formula",
            Price = 50.00m,
            Currency = "USD",
            IsActive = true
        };
        db.Products.Add(prodA);
        await db.SaveChangesAsync();

        // Tenant B attempts to read and update Tenant A's product
        var tenantCtxB = new TenantContext();
        tenantCtxB.SetTenantId(businessBId);
        var handlerB = new ProductHandlers(db, tenantCtxB, new ProductImageService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handlerB.Handle(new GetProductByIdQuery(prodA.Id), CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handlerB.Handle(new UpdateProductCommand(prodA.Id, new UpdateProductRequest(
                Name: "Hijacked",
                Price: 10.00m
            )), CancellationToken.None));
    }

    [Fact]
    public async Task ProductStates_HandlesAllFiveLifecycleStatesCorrectly()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        // 1. Active and in stock
        var prodInStock = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Product 1: Active In Stock",
            Price = 20.00m,
            StockQuantity = 10,
            TrackInventory = true,
            IsActive = true,
            IsPublished = true,
            IsPurchasableOnline = true
        };

        // 2. Active but out of stock
        var prodOutOfStock = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Product 2: Out of Stock",
            Price = 25.00m,
            StockQuantity = 0,
            TrackInventory = true,
            IsActive = true,
            IsPublished = true,
            IsPurchasableOnline = true
        };

        // 3. Inactive
        var prodInactive = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Product 3: Inactive Discontinued",
            Price = 15.00m,
            StockQuantity = 5,
            TrackInventory = true,
            IsActive = false,
            IsPublished = false,
            IsPurchasableOnline = false
        };

        // 4. Not available for online purchase (In-Store Only)
        var prodStoreOnly = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Product 4: In-Store Equipment",
            Price = 150.00m,
            StockQuantity = 3,
            TrackInventory = true,
            IsActive = true,
            IsPublished = true,
            IsPurchasableOnline = false
        };

        // 5. Unpublished (Draft)
        var prodDraft = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Product 5: Upcoming Glow Serum",
            Price = 55.00m,
            StockQuantity = 50,
            TrackInventory = true,
            IsActive = true,
            IsPublished = false,
            IsPurchasableOnline = true
        };

        db.Products.AddRange(prodInStock, prodOutOfStock, prodInactive, prodStoreOnly, prodDraft);
        await db.SaveChangesAsync();

        var allProducts = await handler.Handle(new GetProductsQuery(IncludeInactive: true), CancellationToken.None);
        Assert.Equal(5, allProducts.Count);

        var dto1 = allProducts.First(p => p.Id == prodInStock.Id);
        Assert.Equal("ActiveInStock", dto1.CommercialStatus);
        Assert.Equal("InStock", dto1.InventoryStatus);
        Assert.True(dto1.IsInStock);

        var dto2 = allProducts.First(p => p.Id == prodOutOfStock.Id);
        Assert.Equal("ActiveOutOfStock", dto2.CommercialStatus);
        Assert.Equal("OutOfStock", dto2.InventoryStatus);
        Assert.False(dto2.IsInStock);

        var dto3 = allProducts.First(p => p.Id == prodInactive.Id);
        Assert.Equal("Inactive", dto3.CommercialStatus);

        var dto4 = allProducts.First(p => p.Id == prodStoreOnly.Id);
        Assert.Equal("StoreOnly", dto4.CommercialStatus);
        Assert.False(dto4.IsPurchasableOnline);

        var dto5 = allProducts.First(p => p.Id == prodDraft.Id);
        Assert.Equal("Draft", dto5.CommercialStatus);
        Assert.False(dto5.IsPublished);

        // Filter: In Stock only
        var inStockFilter = await handler.Handle(new GetProductsQuery(Status: "in_stock"), CancellationToken.None);
        Assert.Single(inStockFilter);
        Assert.Equal(prodInStock.Id, inStockFilter[0].Id);

        // Filter: Out of stock only
        var outOfStockFilter = await handler.Handle(new GetProductsQuery(Status: "out_of_stock"), CancellationToken.None);
        Assert.Single(outOfStockFilter);
        Assert.Equal(prodOutOfStock.Id, outOfStockFilter[0].Id);

        // Filter: Store only
        var storeOnlyFilter = await handler.Handle(new GetProductsQuery(Status: "store_only"), CancellationToken.None);
        Assert.Single(storeOnlyFilter);
        Assert.Equal(prodStoreOnly.Id, storeOnlyFilter[0].Id);

        // Filter: Draft / Unpublished
        var draftFilter = await handler.Handle(new GetProductsQuery(Status: "draft"), CancellationToken.None);
        Assert.Single(draftFilter);
        Assert.Equal(prodDraft.Id, draftFilter[0].Id);
    }

    [Fact]
    public async Task InventoryIntegrity_NegativeInventoryIsNeverPermitted()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Integrity Test Cream",
            Price = 30.00m,
            StockQuantity = 10,
            TrackInventory = true,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // 1. Attempting to adjust stock to a negative number throws validation error
        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new AdjustInventoryCommand(product.Id, new AdjustInventoryRequest(-1)), CancellationToken.None));

        // 2. Direct domain method rejects negative stock
        Assert.False(product.AdjustStock(-5));
        Assert.Equal(10, product.StockQuantity);

        // 3. Purchasing more than available stock is rejected
        Assert.False(product.Purchase(15));
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(0, product.SoldQuantity);

        // 4. Purchasing exact available stock leaves 0, never -1
        Assert.True(product.Purchase(10));
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(10, product.SoldQuantity);
        Assert.False(product.Purchase(1)); // Subsequent purchase rejected
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public async Task InventoryPolicy_UntrackedInventoryPermitsUnlimitedPurchases()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        var digitalEbook = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Wellness Guide E-Book",
            Price = 12.00m,
            TrackInventory = false, // Untracked policy
            IsActive = true
        };
        db.Products.Add(digitalEbook);
        await db.SaveChangesAsync();

        Assert.True(digitalEbook.IsInStock);
        Assert.Equal(int.MaxValue, digitalEbook.AvailableQuantity);

        // Purchase succeeds even with 0 stock quantity
        Assert.True(digitalEbook.Purchase(5));
        Assert.Equal(5, digitalEbook.SoldQuantity);
        Assert.True(digitalEbook.Purchase(100));
        Assert.Equal(105, digitalEbook.SoldQuantity);
    }

    [Fact]
    public async Task ImageValidation_InsecureProtocolsRejected()
    {
        var (db, _, handler) = CreateTestContext();

        var requestWithBadUrl = new CreateProductRequest(
            Name: "Hacked Mask",
            Price: 20.00m,
            Images: new List<ProductImageInput>
            {
                new("javascript:alert('xss')", "Exploit", 0, true)
            }
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateProductCommand(requestWithBadUrl), CancellationToken.None));

        Assert.Contains("protocol", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeparationOfDomains_ChangingProductInventoryDoesNotAlterStaffOrAppointments()
    {
        var (db, tenantCtx, handler) = CreateTestContext();

        // Arrange staff and service
        var staff = new DomainStaff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Master Stylist",
            Title = "Specialist",
            IsActive = true
        };
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Signature Haircut",
            DurationMinutes = 60,
            Price = 85.00m,
            IsActive = true
        };
        var booking = new Booking(
            tenantCtx.TenantId,
            staff.Id,
            service.Id,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddMinutes(60)
        );
        booking.Confirm();

        db.Staff.Add(staff);
        db.Services.Add(service);
        db.Bookings.Add(booking);

        var retailProduct = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantCtx.TenantId,
            Name = "Clarifying Shampoo",
            Price = 24.00m,
            StockQuantity = 50,
            TrackInventory = true,
            IsActive = true
        };
        db.Products.Add(retailProduct);
        await db.SaveChangesAsync();

        var initialBookingStart = booking.StartUtc;
        var initialBookingEnd = booking.EndUtc;
        var initialBookingStatus = booking.Status;

        // Act: Perform intense product commerce operations
        await handler.Handle(new AdjustInventoryCommand(retailProduct.Id, new AdjustInventoryRequest(100, "Bulk Restock")), CancellationToken.None);
        retailProduct.Reserve(20);
        retailProduct.Purchase(20);
        await db.SaveChangesAsync();

        // Assert: Appointment schedule, staff availability, and booking intervals remain completely untouched!
        var verifiedBooking = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(verifiedBooking);
        Assert.Equal(initialBookingStart, verifiedBooking.StartUtc);
        Assert.Equal(initialBookingEnd, verifiedBooking.EndUtc);
        Assert.Equal(initialBookingStatus, verifiedBooking.Status);
        Assert.Equal(staff.Id, verifiedBooking.StaffId);

        var verifiedProduct = await db.Products.FindAsync(retailProduct.Id);
        Assert.NotNull(verifiedProduct);
        Assert.Equal(80, verifiedProduct.StockQuantity);
        Assert.Equal(20, verifiedProduct.SoldQuantity);
    }
}
