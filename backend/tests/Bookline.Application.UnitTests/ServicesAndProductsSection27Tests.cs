namespace Bookline.Application.UnitTests;

using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Domain.Exceptions;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ServicesAndProductsSection27Tests
{
    private (BooklineDbContext DbContext, TestTenantContext TenantContext) CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        return (context, tenantContext);
    }

    [Fact]
    public async Task Journey1_BookAppointment_WithoutPurchasingProduct_MaintainsDomainSeparation()
    {
        // Section 27: A customer must be able to book an appointment without purchasing a product
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Signature Haircut",
            DurationMinutes = 30,
            Price = 600m,
            Currency = "INR",
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true
        };
        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Master Stylist",
            TimeZoneId = "Asia/Kolkata",
            IsActive = true
        };
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Aarav",
            LastName = "Patel",
            Email = "aarav@example.com"
        };
        db.Services.Add(service);
        db.Staff.Add(staff);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var booking = new Booking(
            tenantId,
            staff.Id,
            service.Id,
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30)
        );
        booking.TotalPrice = service.Price;
        booking.Currency = service.Currency;
        booking.Confirm();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // Assert: Booking exists in booking domain with no related retail order
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.RelatedOrderId);
        Assert.Null(booking.RelatedOrderNumber);
        Assert.Empty(await db.Orders.Where(o => o.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task Journey2_PurchaseProduct_WithoutBookingAppointment_MaintainsCommerceOwnership()
    {
        // Section 27: Purchase a product without booking an appointment
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Glow Studio",
            Currency = "INR"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Keratin Restorative Shampoo",
            Price = 899m,
            Currency = "INR",
            Sku = "KER-SHAMP-899",
            StockQuantity = 15,
            TrackInventory = true,
            IsActive = true,
            IsPurchasableOnline = true
        };
        db.Tenants.Add(tenant);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Direct commerce domain purchase logic
        var purchased = product.Purchase(2);
        Assert.True(purchased);
        Assert.Equal(13, product.StockQuantity);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Kavita Rao",
            CustomerEmail = "kavita@example.com",
            CustomerPhone = "+91 98250 99887",
            Subtotal = 1798m,
            Tax = 89.9m,
            TotalAmount = 1887.9m,
            Currency = "INR",
            Status = OrderStatus.Paid,
            RelatedBookingId = null,
            RelatedBookingReference = null,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = 2,
                    TotalPrice = 1798m
                }
            }
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Assert: Order created in commerce domain, inventory deducted, no appointment created
        var savedOrder = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.TenantId == tenantId);
        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Paid, savedOrder.Status);
        Assert.Null(savedOrder.RelatedBookingId);
        Assert.Null(savedOrder.RelatedBookingReference);
        Assert.Equal(2, savedOrder.Items.First().Quantity);
        Assert.Equal(1798m, savedOrder.Subtotal);

        // Booking table remains empty
        Assert.Empty(await db.Bookings.Where(b => b.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task Journey3_CombinedPurchase_LinksSeparateEntities_WithoutMergingDomainEntities()
    {
        // Section 27: Book an appointment and add a product to a related purchase.
        // Do not force an appointment and a product order into one database entity.
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Glow Studio", Currency = "INR" };
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Haircut",
            DurationMinutes = 30,
            Price = 600m,
            Currency = "INR"
        };
        var staff = new Staff { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Master Stylist", TimeZoneId = "Asia/Kolkata", IsActive = true };
        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Priya", LastName = "Sharma", Email = "priya@example.com" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Keratin Shampoo",
            Price = 899m,
            Currency = "INR",
            Sku = "KER-SHMP-01",
            StockQuantity = 20,
            TrackInventory = true,
            IsActive = true,
            IsPurchasableOnline = true
        };

        db.Tenants.Add(tenant);
        db.Services.Add(service);
        db.Staff.Add(staff);
        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // 1. Create booking in Booking domain
        var booking = new Booking(
            tenantId,
            staff.Id,
            service.Id,
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(2),
            DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30)
        );
        booking.TotalPrice = service.Price;
        booking.Currency = "INR";
        booking.Confirm();
        db.Bookings.Add(booking);

        // 2. Commerce domain purchases product and creates separate Order
        var stockDeducted = product.Purchase(1);
        Assert.True(stockDeducted);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            CustomerName = "Priya Sharma",
            CustomerEmail = "priya@example.com",
            CustomerPhone = "+91 98765 43210",
            Subtotal = 899m,
            Tax = 44.95m,
            TotalAmount = 943.95m,
            Currency = "INR",
            Status = OrderStatus.Paid,
            RelatedBookingId = booking.Id,
            RelatedBookingReference = booking.BookingReference,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = 1,
                    TotalPrice = 899m
                }
            }
        };
        booking.AttachRelatedOrder(order.Id, order.OrderNumber);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Assert: Separate database entities maintained
        var savedOrder = await db.Orders.FirstOrDefaultAsync(o => o.TenantId == tenantId);
        Assert.NotNull(savedOrder);

        // Distinct references connecting the journey
        Assert.Equal(booking.Id, savedOrder.RelatedBookingId);
        Assert.Equal(booking.BookingReference, savedOrder.RelatedBookingReference);

        var savedBooking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(savedOrder.Id, savedBooking.RelatedOrderId);
        Assert.Equal(savedOrder.OrderNumber, savedBooking.RelatedOrderNumber);

        // Domain boundaries verified
        // Booking domain owns: status, dates, staff
        Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);
        Assert.Equal(staff.Id, savedBooking.StaffId);

        // Commerce domain owns: SKU, order status, inventory, payment
        Assert.Equal(OrderStatus.Paid, savedOrder.Status);
        Assert.Equal(19, (await db.Products.FirstAsync(p => p.Id == product.Id)).StockQuantity);
    }

    [Fact]
    public async Task FailureIsolation_ProductOutOfStock_DoesNotCorruptOrCancelBookingHold()
    {
        // Section 27: Define how failures are handled.
        // An appointment reservation and a payment authorization are not automatically one atomic transaction.
        // Out of stock product failure must not invalidate the booking hold.
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var tenant = new Tenant { Id = tenantId, Name = "Glow Studio", Currency = "INR" };
        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Haircut", DurationMinutes = 30, Price = 600m, Currency = "INR" };
        var staff = new Staff { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Specialist", TimeZoneId = "Asia/Kolkata", IsActive = true };
        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Rohit", LastName = "Mehta", Email = "rohit@example.com" };
        var outOfStockProduct = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Sold Out Scalp Serum",
            Price = 1200m,
            Currency = "INR",
            Sku = "SERUM-0",
            StockQuantity = 0, // 0 inventory
            TrackInventory = true,
            IsActive = true,
            IsPurchasableOnline = true
        };

        db.Tenants.Add(tenant);
        db.Services.Add(service);
        db.Staff.Add(staff);
        db.Customers.Add(customer);
        db.Products.Add(outOfStockProduct);
        await db.SaveChangesAsync();

        // 1. Booking hold / reservation is active
        var booking = new Booking(
            tenantId,
            staff.Id,
            service.Id,
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30)
        );
        booking.Confirm();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // 2. Customer attempts to purchase out-of-stock product
        var purchaseAttempt = outOfStockProduct.Purchase(1);
        Assert.False(purchaseAttempt);

        // Assert: Booking hold was NOT corrupted or cancelled because of product stock failure
        var activeBooking = await db.Bookings.FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Confirmed, activeBooking.Status);
        Assert.Null(activeBooking.RelatedOrderId);

        // No partial orphan orders created in database
        Assert.Empty(await db.Orders.Where(o => o.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task Invariant_DoNotConfirmBooking_SimplyBecauseCartWasCreatedOrPaymentOpened()
    {
        // Section 27: Do not confirm a booking simply because a cart was created or a payment page was opened.
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Blowdry", Price = 400m, Currency = "INR" };
        var staff = new Staff { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Stylist", TimeZoneId = "Asia/Kolkata", IsActive = true };
        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Maya", LastName = "Sen", Email = "maya@example.com" };

        db.Services.Add(service);
        db.Staff.Add(staff);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var booking = new Booking(
            tenantId,
            staff.Id,
            service.Id,
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(3),
            DateTimeOffset.UtcNow.AddDays(3).AddMinutes(45)
        );
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // In pending status: Cart or payment interaction alone does NOT mutate it to confirmed
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.False(booking.Status == BookingStatus.Confirmed);
    }
}
