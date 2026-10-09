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
}
