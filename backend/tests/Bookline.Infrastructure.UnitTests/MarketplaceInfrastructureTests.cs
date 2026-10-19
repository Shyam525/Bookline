using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Geo;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookline.Infrastructure.UnitTests;

public class MarketplaceInfrastructureTests
{
    private BooklineDbContext CreateInMemoryContext()
    {
        var tenantContext = new Bookline.Application.Common.Models.TenantContext();
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task GeocodingProvider_ShouldResolveKnownCitiesAccurately()
    {
        // Arrange
        var provider = new GeocodingProvider();

        // Act
        var ahmedabad = await provider.GeocodeAsync("Ahmedabad");
        var mumbai = await provider.GeocodeAsync("Mumbai");
        var reverse = await provider.ReverseGeocodeAsync(23.0225, 72.5714);

        // Assert
        Assert.NotNull(ahmedabad);
        Assert.Equal(23.0225, ahmedabad.Latitude);
        Assert.Equal(72.5714, ahmedabad.Longitude);

        Assert.NotNull(mumbai);
        Assert.Equal(19.0760, mumbai.Latitude);

        Assert.Equal("Ahmedabad", reverse);
    }

    [Fact]
    public void Haversine_Distance_Calculation_ShouldBeAccurate()
    {
        // Ahmedabad to Rajkot
        double dist = GeocodingProvider.CalculateDistanceKm(23.0225, 72.5714, 22.3039, 70.8022);

        // Distance is ~200-220 km
        Assert.True(dist > 190 && dist < 230, $"Distance was {dist} km");
    }

    [Fact]
    public async Task ProviderSearchService_ShouldFilterAndRankProvidersCorrectly()
    {
        // Arrange
        var db = CreateInMemoryContext();

        var t1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Aura Luxury Wellness",
            Slug = "aura-wellness",
            Category = "Beauty & Wellness",
            BusinessType = "Spa",
            City = "Ahmedabad",
            Latitude = 23.0396,
            Longitude = 72.5074,
            AverageRating = 4.9,
            ReviewCount = 120,
            VerificationStatus = VerificationStatus.Verified,
            IsPublished = true,
            IsActive = true
        };

        var t2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Zenith Fitness Gym",
            Slug = "zenith-fitness",
            Category = "Fitness & Training",
            BusinessType = "Pilates",
            City = "Mumbai",
            Latitude = 19.0760,
            Longitude = 72.8777,
            AverageRating = 4.5,
            ReviewCount = 30,
            VerificationStatus = VerificationStatus.Unverified,
            IsPublished = true,
            IsActive = true
        };

        db.Tenants.AddRange(t1, t2);

        var s1 = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Name = "Aroma Facial",
            Price = 60.00m,
            IsActive = true
        };
        db.Services.Add(s1);

        await db.SaveChangesAsync();

        var searchService = new ProviderSearchService(db, new SearchIntentService());

        // Act 1: Search by category Beauty
        var catResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery
        {
            Category = "Beauty"
        });

        // Assert 1
        Assert.Single(catResult.Items);
        Assert.Equal("Aura Luxury Wellness", catResult.Items[0].Name);
        Assert.Equal(60.00m, catResult.Items[0].StartingPrice);

        // Act 2: Search with coordinates near Ahmedabad
        var geoResult = await searchService.SearchProvidersAsync(new ProviderSearchQuery
        {
            Latitude = 23.0390,
            Longitude = 72.5070,
            RadiusKm = 10
        });

        // Assert 2
        Assert.Single(geoResult.Items);
        Assert.Equal("Aura Luxury Wellness", geoResult.Items[0].Name);
        Assert.NotNull(geoResult.Items[0].DistanceKm);
        Assert.True(geoResult.Items[0].DistanceKm < 2.0);
    }

    [Fact]
    public async Task PaymentProvider_ProcessPayment_ShouldCalculateCommissionAndCreditPayoutBalances()
    {
        // Sections 85, 88, 89: ₹1,000 paid -> 10% commission -> ₹900 provider net
        var db = CreateInMemoryContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Studio",
            Slug = "glow-studio",
            CommissionRatePercentage = 10.00m,
            PendingPayoutBalance = 0m,
            AvailablePayoutBalance = 0m,
            Currency = "INR"
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var paymentProvider = new Bookline.Infrastructure.Payments.PaymentProvider(db);
        var result = await paymentProvider.ProcessPaymentAsync(new ProcessPaymentRequest(
            TenantId: tenant.Id,
            CustomerId: Guid.NewGuid(),
            BookingId: null,
            OrderId: Guid.NewGuid(),
            Amount: 1000.00m,
            Currency: "INR",
            PaymentMethod: "Demo"
        ));

        Assert.True(result.Success);
        Assert.Equal("Succeeded", result.Status);
        Assert.NotEqual(Guid.Empty, result.PaymentId);

        // Verify balances & commission
        var updatedTenant = await db.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updatedTenant);
        Assert.Equal(900.00m, updatedTenant.PendingPayoutBalance);
        Assert.Equal(900.00m, updatedTenant.AvailablePayoutBalance);

        // Verify OutboxMessage for PaymentSucceeded (Section 90 & 91)
        var outboxMsg = await db.OutboxMessages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.EventType == Bookline.Domain.Constants.NotificationEvents.PaymentSucceeded);
        Assert.NotNull(outboxMsg);
    }

    [Fact]
    public async Task PaymentProvider_ProcessRefund_ShouldPersistRefundEntityAndAdjustBalance()
    {
        // Sections 87 & 88
        var db = CreateInMemoryContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Studio",
            Slug = "glow-studio",
            CommissionRatePercentage = 10.00m,
            AvailablePayoutBalance = 900.00m,
            Currency = "INR"
        };
        db.Tenants.Add(tenant);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Amount = 1000.00m,
            Currency = "INR",
            Status = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.CreditCard
        };
        db.Payments.Add(payment);

        var commission = Commission.Calculate(tenant.Id, 1000.00m, 10.00m, "INR", payment.Id);
        db.Commissions.Add(commission);
        await db.SaveChangesAsync();

        var paymentProvider = new Bookline.Infrastructure.Payments.PaymentProvider(db);
        var refundResult = await paymentProvider.ProcessRefundAsync(new ProcessRefundRequest(
            PaymentId: payment.Id,
            Amount: 1000.00m,
            Reason: "Customer cancellation request"
        ));

        Assert.True(refundResult.Success);
        Assert.Equal("Refunded", refundResult.Status);

        // Verify first-class Refund entity created (Section 87)
        var refundEntity = await db.Refunds.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PaymentId == payment.Id);
        Assert.NotNull(refundEntity);
        Assert.Equal(1000.00m, refundEntity.Amount);
        Assert.Equal("Succeeded", refundEntity.Status);
        Assert.NotEmpty(refundEntity.ExternalReference);

        // Verify provider balance deducted
        var updatedTenant = await db.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updatedTenant);
        Assert.Equal(0.00m, updatedTenant.AvailablePayoutBalance);
    }

    [Fact]
    public async Task PayoutProvider_DisbursePayout_ShouldReduceAvailableBalanceAndTrackPaidOut()
    {
        // Section 89
        var db = CreateInMemoryContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Studio",
            Slug = "glow-studio",
            AvailablePayoutBalance = 5000.00m,
            PaidOutBalance = 0m,
            Currency = "INR"
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var payoutProvider = new Bookline.Infrastructure.Payments.PayoutProvider(db);
        var result = await payoutProvider.DisbursePayoutAsync(new ProcessPayoutRequest(
            TenantId: tenant.Id,
            Amount: 3000.00m,
            Currency: "INR",
            Method: "BankTransfer",
            DestinationAccount: "HDFC Acc 1234"
        ));

        Assert.True(result.Success);
        Assert.Equal("Paid", result.Status);

        var updatedTenant = await db.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updatedTenant);
        Assert.Equal(2000.00m, updatedTenant.AvailablePayoutBalance);
        Assert.Equal(3000.00m, updatedTenant.PaidOutBalance);

        var payoutRecord = await db.Payouts.FindAsync(result.PayoutId);
        Assert.NotNull(payoutRecord);
        Assert.Equal(3000.00m, payoutRecord.Amount);
    }

    [Fact]
    public async Task TeamAuthorizationService_ShouldEnforceRoleHierarchyAndPermissions()
    {
        // Section 95: Owner, Admin, Manager, Receptionist, Staff, Viewer
        var db = CreateInMemoryContext();
        var tenantId = Guid.NewGuid();

        var ownerUser = new AppUser { Id = Guid.NewGuid(), Email = "owner@test.com", Role = "Owner", TenantId = tenantId };
        var adminUser = new AppUser { Id = Guid.NewGuid(), Email = "admin@test.com", Role = "Staff", TenantId = tenantId };
        var managerUser = new AppUser { Id = Guid.NewGuid(), Email = "mgr@test.com", Role = "Staff", TenantId = tenantId };
        var receptionistUser = new AppUser { Id = Guid.NewGuid(), Email = "rec@test.com", Role = "Staff", TenantId = tenantId };
        var viewerUser = new AppUser { Id = Guid.NewGuid(), Email = "viewer@test.com", Role = "Staff", TenantId = tenantId };

        db.Users.AddRange(ownerUser, adminUser, managerUser, receptionistUser, viewerUser);

        db.OrganizationMemberships.AddRange(
            new OrganizationMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = ownerUser.Id, Role = "Owner" },
            new OrganizationMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = adminUser.Id, Role = "Admin" },
            new OrganizationMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = managerUser.Id, Role = "Manager" },
            new OrganizationMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = receptionistUser.Id, Role = "Receptionist" },
            new OrganizationMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = viewerUser.Id, Role = "Viewer" }
        );

        await db.SaveChangesAsync();

        var teamAuth = new Bookline.Infrastructure.Services.TeamAuthorizationService(db);

        // Owner checks
        Assert.True(await teamAuth.CanManageTeamAsync(ownerUser.Id, tenantId));
        Assert.True(await teamAuth.CanManageFinancialsAsync(ownerUser.Id, tenantId));
        Assert.True(await teamAuth.CanMutateDataAsync(ownerUser.Id, tenantId));

        // Admin checks
        Assert.True(await teamAuth.CanManageTeamAsync(adminUser.Id, tenantId));
        Assert.False(await teamAuth.CanManageFinancialsAsync(adminUser.Id, tenantId)); // Financials reserved for Owner

        // Manager checks
        Assert.False(await teamAuth.CanManageTeamAsync(managerUser.Id, tenantId));
        Assert.True(await teamAuth.CanManageServicesAsync(managerUser.Id, tenantId));
        Assert.True(await teamAuth.CanManageStaffAsync(managerUser.Id, tenantId));

        // Receptionist checks
        Assert.False(await teamAuth.CanManageServicesAsync(receptionistUser.Id, tenantId));
        Assert.True(await teamAuth.CanManageBookingsAsync(receptionistUser.Id, tenantId));

        // Viewer checks (read-only)
        Assert.False(await teamAuth.CanManageBookingsAsync(viewerUser.Id, tenantId));
        Assert.False(await teamAuth.CanMutateDataAsync(viewerUser.Id, tenantId));
    }

    [Fact]
    public async Task MarketplaceDbSeeder_ShouldSeedDeterministicGlowStudioEnvironment_WithAllEntitiesAndDemoAccount()
    {
        // Phase 42 & Phase 43
        var db = CreateInMemoryContext();
        var hasher = new Bookline.Infrastructure.Authentication.PasswordHasher();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger);

        var glowId = Guid.Parse("30000000-0000-0000-0000-000000000002");

        // 1. Organization
        var glowTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == glowId);
        Assert.NotNull(glowTenant);
        Assert.Equal("Glow Studio", glowTenant.Name);
        Assert.Equal("glow-studio", glowTenant.Slug);
        Assert.Equal("INR", glowTenant.Currency);
        Assert.Equal("Ahmedabad", glowTenant.City);

        // 2. Locations: Ahmedabad and Surat
        var locations = await db.Locations.IgnoreQueryFilters().Where(l => l.TenantId == glowId).ToListAsync();
        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, l => l.City == "Ahmedabad");
        Assert.Contains(locations, l => l.City == "Surat");

        // 3. Staff: multiple realistic staff
        var staff = await db.Staff.IgnoreQueryFilters().Where(s => s.TenantId == glowId).ToListAsync();
        Assert.True(staff.Count >= 4);
        Assert.Contains(staff, s => s.Name == "Priya Sharma");
        Assert.Contains(staff, s => s.Name == "Aarav Mehta");
        Assert.Contains(staff, s => s.Name == "Diya Patel");
        Assert.Contains(staff, s => s.Name == "Rohan Shah");

        // 4. Working hours & Breaks
        var workingHours = await db.WorkingHours.IgnoreQueryFilters().Where(w => w.TenantId == glowId).ToListAsync();
        Assert.NotEmpty(workingHours);
        Assert.Contains(workingHours, w => w.StartTime == new TimeOnly(9, 0) && w.EndTime == new TimeOnly(13, 0));
        Assert.Contains(workingHours, w => w.StartTime == new TimeOnly(14, 0) && w.EndTime == new TimeOnly(19, 0));

        // 5. Time Off
        var timeOffs = await db.TimeOffs.IgnoreQueryFilters().Where(t => t.TenantId == glowId).ToListAsync();
        Assert.NotEmpty(timeOffs);

        // 6. Services: Exact 7 services
        var services = await db.Services.IgnoreQueryFilters().Where(s => s.TenantId == glowId).ToListAsync();
        Assert.Equal(7, services.Count);
        var serviceNames = services.Select(s => s.Name).ToHashSet();
        Assert.Contains("Haircut", serviceNames);
        Assert.Contains("Haircut & Style", serviceNames);
        Assert.Contains("Full Colour", serviceNames);
        Assert.Contains("Facial", serviceNames);
        Assert.Contains("Massage", serviceNames);
        Assert.Contains("Consultation", serviceNames);
        Assert.Contains("Keratin Treatment", serviceNames);

        // 7. Appointments: Mix of states
        var bookings = await db.Bookings.IgnoreQueryFilters().Where(b => b.TenantId == glowId).ToListAsync();
        Assert.Contains(bookings, b => b.Status == BookingStatus.Confirmed);
        Assert.Contains(bookings, b => b.Status == BookingStatus.Completed);
        Assert.Contains(bookings, b => b.Status == BookingStatus.Cancelled);
        Assert.Contains(bookings, b => b.Status == BookingStatus.NoShow);

        var holds = await db.BookingHolds.IgnoreQueryFilters().Where(h => h.TenantId == glowId).ToListAsync();
        Assert.NotEmpty(holds);

        // 8. Customers
        var customers = await db.Customers.IgnoreQueryFilters().Where(c => c.TenantId == glowId).ToListAsync();
        Assert.True(customers.Count >= 4);

        // 9. Notifications & Audit Events
        var notifications = await db.NotificationLogs.IgnoreQueryFilters().Where(n => n.TenantId == glowId).ToListAsync();
        Assert.NotEmpty(notifications);

        var auditLogs = await db.AuditLogs.IgnoreQueryFilters().Where(a => a.TenantId == glowId).ToListAsync();
        Assert.NotEmpty(auditLogs);

        // 10. Demo Account (Phase 43)
        var demoUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "demo@bookline.local");
        Assert.NotNull(demoUser);
        Assert.Equal(glowId, demoUser.TenantId);
        Assert.Equal("Owner", demoUser.Role);
        Assert.True(hasher.VerifyPassword("BooklineDemo123!", demoUser.PasswordHash));

        var demoMembership = await db.OrganizationMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == demoUser.Id && m.TenantId == glowId);
        Assert.NotNull(demoMembership);
        Assert.Equal("Owner", demoMembership.Role);
    }

    [Fact]
    public async Task MarketplaceDbSeeder_WhenEnvironmentIsProduction_ThrowsInvalidOperationException()
    {
        // Section 116 & 117: Environment-gated — Demo seeding must be refused in Production
        var db = CreateInMemoryContext();
        var hasher = new Bookline.Infrastructure.Authentication.PasswordHasher();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger, "Production"));

        Assert.Contains("prohibited in the Production environment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarketplaceDbSeeder_WhenRunTwice_IsIdempotentAndProducesNoDuplicates()
    {
        // Section 117: Idempotency guarantee — Running seed twice must not duplicate records
        var db = CreateInMemoryContext();
        var hasher = new Bookline.Infrastructure.Authentication.PasswordHasher();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        // Run 1
        await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger, "Development");
        var tenantCount1 = await db.Tenants.IgnoreQueryFilters().CountAsync();
        var userCount1 = await db.Users.IgnoreQueryFilters().CountAsync();
        var locationCount1 = await db.Locations.IgnoreQueryFilters().CountAsync();
        var serviceCount1 = await db.Services.IgnoreQueryFilters().CountAsync();
        var bookingCount1 = await db.Bookings.IgnoreQueryFilters().CountAsync();

        // Run 2
        await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger, "Development");
        var tenantCount2 = await db.Tenants.IgnoreQueryFilters().CountAsync();
        var userCount2 = await db.Users.IgnoreQueryFilters().CountAsync();
        var locationCount2 = await db.Locations.IgnoreQueryFilters().CountAsync();
        var serviceCount2 = await db.Services.IgnoreQueryFilters().CountAsync();
        var bookingCount2 = await db.Bookings.IgnoreQueryFilters().CountAsync();

        Assert.Equal(tenantCount1, tenantCount2);
        Assert.Equal(userCount1, userCount2);
        Assert.Equal(locationCount1, locationCount2);
        Assert.Equal(serviceCount1, serviceCount2);
        Assert.Equal(bookingCount1, bookingCount2);
    }

    [Fact]
    public async Task MarketplaceDbSeeder_ContainsAllFiveRequiredCities_WithDeliberateCoordinates()
    {
        // Section 115: Required example cities: Ahmedabad, Rajkot, Surat, Mumbai, Bangalore
        var db = CreateInMemoryContext();
        var hasher = new Bookline.Infrastructure.Authentication.PasswordHasher();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger, "Development");

        var cities = await db.Locations.IgnoreQueryFilters()
            .Select(l => l.City)
            .Distinct()
            .ToListAsync();

        Assert.Contains("Ahmedabad", cities);
        Assert.Contains("Rajkot", cities);
        Assert.Contains("Surat", cities);
        Assert.Contains("Mumbai", cities);
        Assert.Contains("Bangalore", cities);

        // Verify all locations have valid, deliberate coordinates
        var locations = await db.Locations.IgnoreQueryFilters().ToListAsync();
        foreach (var loc in locations)
        {
            Assert.True(loc.Latitude > 0, $"Location {loc.Name} has invalid latitude {loc.Latitude}");
            Assert.True(loc.Longitude > 0, $"Location {loc.Name} has invalid longitude {loc.Longitude}");
        }
    }

    [Fact]
    public async Task MarketplaceDbSeeder_SeedsAllThreeDemoIdentities_WithCorrectRolesAndAccess()
    {
        // Section 116: customer@bookline.local, provider@bookline.local, admin@bookline.local
        var db = CreateInMemoryContext();
        var hasher = new Bookline.Infrastructure.Authentication.PasswordHasher();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, hasher, logger, "Development");

        // 1. Customer Persona
        var customer = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "customer@bookline.local");
        Assert.NotNull(customer);
        Assert.Equal("Customer", customer.Role);
        Assert.True(hasher.VerifyPassword("Customer123!", customer.PasswordHash));

        // 2. Provider Persona with business membership
        var provider = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "provider@bookline.local");
        Assert.NotNull(provider);
        Assert.Equal("Owner", provider.Role);
        Assert.True(hasher.VerifyPassword("Provider123!", provider.PasswordHash));
        var memberships = await db.OrganizationMemberships.IgnoreQueryFilters()
            .Where(m => m.UserId == provider.Id)
            .ToListAsync();
        Assert.NotEmpty(memberships);
        Assert.All(memberships, m => Assert.Equal("Owner", m.Role));

        // 3. Admin Persona
        var admin = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "admin@bookline.local");
        Assert.NotNull(admin);
        Assert.Equal("PlatformAdmin", admin.Role);
        Assert.True(hasher.VerifyPassword("Admin123!", admin.PasswordHash));
    }
}
