using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Persistence.Seed;

public static class MarketplaceDbSeeder
{
    public static async Task SeedAsync(BooklineDbContext db, IPasswordHasher passwordHasher, ILogger logger, string environmentName = "Development")
    {
        // Section 116 & 117: Strictly refuse and guard against demo account seeding in Production
        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Demo seeding was requested but is strictly prohibited in the Production environment.");
            throw new InvalidOperationException("Demo seeding is prohibited in the Production environment.");
        }

        try
        {
            // 1. Seed Demo Accounts (Section 116: customer@bookline.local, provider@bookline.local, admin@bookline.local)
            var adminEmail = "admin@bookline.local";
            var providerEmail = "provider@bookline.local";
            var customerEmail = "customer@bookline.local";

            var demoAdminPassword = Environment.GetEnvironmentVariable("BOOKLINE_DEMO_ADMIN_PASSWORD") ?? "Admin123!";
            var demoProviderPassword = Environment.GetEnvironmentVariable("BOOKLINE_DEMO_PROVIDER_PASSWORD") ?? "Provider123!";
            var demoCustomerPassword = Environment.GetEnvironmentVariable("BOOKLINE_DEMO_CUSTOMER_PASSWORD") ?? "Customer123!";

            var adminUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    TenantId = Guid.Empty,
                    Email = adminEmail,
                    PasswordHash = passwordHasher.HashPassword(demoAdminPassword),
                    FirstName = "Platform",
                    LastName = "Administrator",
                    Role = "PlatformAdmin",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(adminUser);
            }

            var customerUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == customerEmail);
            if (customerUser == null)
            {
                customerUser = new AppUser
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    TenantId = Guid.Empty,
                    Email = customerEmail,
                    PasswordHash = passwordHasher.HashPassword(demoCustomerPassword),
                    FirstName = "Jane",
                    LastName = "Customer",
                    Phone = "+91 98765 43210",
                    Role = "Customer",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(customerUser);
            }

            var rohitEmail = "rohit.patel@bookline.local";
            var rohitUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == rohitEmail);
            if (rohitUser == null)
            {
                rohitUser = new AppUser
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    TenantId = Guid.Empty,
                    Email = rohitEmail,
                    PasswordHash = passwordHasher.HashPassword("Customer123!"),
                    FirstName = "Rohit",
                    LastName = "Patel",
                    Phone = "+91 98240 55667",
                    Role = "Customer",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(rohitUser);
            }

            // 2. Seed Multi-Vendor Providers across all 8 Categories (Section 125)
            var auraId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            var glowId = Guid.Parse("30000000-0000-0000-0000-000000000002");
            var apexId = Guid.Parse("30000000-0000-0000-0000-000000000003");
            var zenithId = Guid.Parse("30000000-0000-0000-0000-000000000004");
            var luminaId = Guid.Parse("30000000-0000-0000-0000-000000000005");
            var vanguardId = Guid.Parse("30000000-0000-0000-0000-000000000006");
            var cambridgeId = Guid.Parse("30000000-0000-0000-0000-000000000007");
            var urbanFixId = Guid.Parse("30000000-0000-0000-0000-000000000008");

            var auraTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == auraId);
            if (auraTenant == null)
            {
                auraTenant = new Tenant
                {
                    Id = auraId,
                    Name = "Aura Wellness & Spa",
                    Slug = "aura-wellness",
                    Category = "Wellness",
                    BusinessType = "Wellness & Aesthetic Spa",
                    Description = "Luxury holistic sanctuary offering restorative skin rituals, aromatherapy treatments, and serene therapeutic spaces.",
                    Address = "102 Sindhu Bhavan Marg, Bodakdev",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380054",
                    Latitude = 23.0396,
                    Longitude = 72.5074,
                    Phone = "+91 79 4001 8899",
                    Website = "https://aura-wellness.bookline.local",
                    AverageRating = 4.9,
                    ReviewCount = 142,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.Fixed,
                    DepositAmount = 25.00m,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 1450.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-6)
                };
                db.Tenants.Add(auraTenant);
            }

            var glowTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == glowId);
            if (glowTenant == null)
            {
                glowTenant = new Tenant
                {
                    Id = glowId,
                    Name = "Glow Studio",
                    Slug = "glow-studio",
                    Category = "Beauty",
                    BusinessType = "Editorial Hair & Beauty Studio",
                    Description = "High-fashion hair artistry, precision barbering, and clinical keratin hair care by international master stylists.",
                    Address = "Sindhu Bhavan Road, Bodakdev",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380054",
                    Latitude = 23.0396,
                    Longitude = 72.5074,
                    Phone = "+91 79 4001 9900",
                    Website = "https://glowstudio.bookline.local",
                    AverageRating = 4.9,
                    ReviewCount = 112,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "INR",
                    DepositType = DepositType.Percentage,
                    DepositAmount = 20.00m,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 980.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-5)
                };
                db.Tenants.Add(glowTenant);
            }

            var apexTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == apexId);
            if (apexTenant == null)
            {
                apexTenant = new Tenant
                {
                    Id = apexId,
                    Name = "Apex Dental & Aesthetics",
                    Slug = "apex-dental",
                    Category = "Healthcare",
                    BusinessType = "Cosmetic Dental Clinic",
                    Description = "Painless laser dentistry, clear aligners, and precision porcelain veneers in a clinical luxury suite.",
                    Address = "401 Iscon Elegance, Prahlad Nagar",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380015",
                    Latitude = 23.0076,
                    Longitude = 72.5042,
                    Phone = "+91 79 2692 1100",
                    Website = "https://apexdental.bookline.local",
                    AverageRating = 4.95,
                    ReviewCount = 86,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.None,
                    CommissionRatePercentage = 8.00m,
                    AvailablePayoutBalance = 2340.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-4)
                };
                db.Tenants.Add(apexTenant);
            }

            var zenithTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == zenithId);
            if (zenithTenant == null)
            {
                zenithTenant = new Tenant
                {
                    Id = zenithId,
                    Name = "Zenith Fitness & Reformer Pilates",
                    Slug = "zenith-fitness",
                    Category = "Fitness",
                    BusinessType = "Reformer Pilates Sanctuary",
                    Description = "Dynamic reformer pilates sessions, private athletic conditioning, and athletic recovery.",
                    Address = "88 100 Feet Road, Indiranagar",
                    City = "Bangalore",
                    State = "Karnataka",
                    Country = "India",
                    PostalCode = "560038",
                    Latitude = 12.9784,
                    Longitude = 77.6408,
                    Phone = "+91 80 4123 9900",
                    AverageRating = 4.7,
                    ReviewCount = 64,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.None,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 820.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-3)
                };
                db.Tenants.Add(zenithTenant);
            }

            var luminaTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == luminaId);
            if (luminaTenant == null)
            {
                luminaTenant = new Tenant
                {
                    Id = luminaId,
                    Name = "Lumina Portraiture Studio",
                    Slug = "lumina-studio",
                    Category = "Photography",
                    BusinessType = "Commercial Photography Studio",
                    Description = "High-end corporate headshots, editorial campaigns, and bespoke family heirloom portraiture.",
                    Address = "22 Dumas Road, Piplod",
                    City = "Surat",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "395007",
                    Latitude = 21.1610,
                    Longitude = 72.7758,
                    Phone = "+91 261 278 4400",
                    AverageRating = 4.85,
                    ReviewCount = 41,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.Percentage,
                    DepositAmount = 30.00m,
                    CommissionRatePercentage = 12.00m,
                    AvailablePayoutBalance = 650.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-2)
                };
                db.Tenants.Add(luminaTenant);
            }

            var vanguardTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == vanguardId);
            if (vanguardTenant == null)
            {
                vanguardTenant = new Tenant
                {
                    Id = vanguardId,
                    Name = "Vanguard Legal & Business Advisory",
                    Slug = "vanguard-advisory",
                    Category = "Consulting",
                    BusinessType = "Corporate Legal Consultancy",
                    Description = "Strategic corporate law, IP management, and cross-border commercial contracts counsel.",
                    Address = "502 Imperial Heights, Yagnik Road",
                    City = "Rajkot",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "360001",
                    Latitude = 22.2980,
                    Longitude = 70.7960,
                    Phone = "+91 281 245 7711",
                    AverageRating = 4.9,
                    ReviewCount = 33,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.None,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 1200.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-2)
                };
                db.Tenants.Add(vanguardTenant);
            }

            var cambridgeTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == cambridgeId);
            if (cambridgeTenant == null)
            {
                cambridgeTenant = new Tenant
                {
                    Id = cambridgeId,
                    Name = "Cambridge Academic Coaching & Tutoring",
                    Slug = "cambridge-academy",
                    Category = "Education",
                    BusinessType = "Academic Tutoring & Prep",
                    Description = "Expert collegiate entrance coaching, test prep, and personalized STEM tutoring by Ivy-league mentors.",
                    Address = "44 Residency Road, Richmond Town",
                    City = "Bangalore",
                    State = "Karnataka",
                    Country = "India",
                    PostalCode = "560025",
                    Latitude = 12.9667,
                    Longitude = 77.6000,
                    Phone = "+91 80 4910 2211",
                    AverageRating = 4.92,
                    ReviewCount = 55,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.None,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 950.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-3)
                };
                db.Tenants.Add(cambridgeTenant);
            }

            var urbanFixTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == urbanFixId);
            if (urbanFixTenant == null)
            {
                urbanFixTenant = new Tenant
                {
                    Id = urbanFixId,
                    Name = "UrbanFix Pro Home Services",
                    Slug = "urbanfix-home",
                    Category = "Home Services",
                    BusinessType = "Residential Repair & Installation",
                    Description = "Vetted electricians, smart home automation setup, HVAC tune-ups, and plumbing diagnostics.",
                    Address = "88 Navrangpura Cross Road, CG Road",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380009",
                    Latitude = 23.0365,
                    Longitude = 72.5611,
                    Phone = "+91 79 4010 3344",
                    AverageRating = 4.78,
                    ReviewCount = 72,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
                    DepositType = DepositType.None,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 1100.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-2)
                };
                db.Tenants.Add(urbanFixTenant);
            }

            // Section 115: 9th Vendor in Mumbai across deliberate geographic coordinates
            var mumbaiId = Guid.Parse("30000000-0000-0000-0000-000000000009");
            var mumbaiTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == mumbaiId);
            if (mumbaiTenant == null)
            {
                mumbaiTenant = new Tenant
                {
                    Id = mumbaiId,
                    Name = "Verdant Sanctuary & Longevity Suite",
                    Slug = "verdant-sanctuary",
                    Category = "Wellness",
                    BusinessType = "Longevity & Wellness Suite",
                    Description = "Advanced hyperbaric oxygen therapy, cryotherapy chambers, and integrative functional medicine.",
                    Address = "28 Waterfield Road, Bandra West",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Country = "India",
                    PostalCode = "400050",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    Phone = "+91 22 4901 8899",
                    Website = "https://verdant-sanctuary.bookline.local",
                    AverageRating = 4.96,
                    ReviewCount = 48,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "INR",
                    DepositType = DepositType.Fixed,
                    DepositAmount = 500.00m,
                    CommissionRatePercentage = 10.00m,
                    AvailablePayoutBalance = 3200.00m,
                    IsPublished = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-4)
                };
                db.Tenants.Add(mumbaiTenant);
            }

            await db.SaveChangesAsync();

            // 3. Provider User with Multi-Business Ownership (Section 116: provider@bookline.local)
            var providerUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == providerEmail);
            if (providerUser == null)
            {
                providerUser = new AppUser
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000099"),
                    TenantId = auraId, // Primary active business
                    Email = providerEmail,
                    PasswordHash = passwordHasher.HashPassword(demoProviderPassword),
                    FirstName = "Aarav",
                    LastName = "Singhania",
                    Phone = "+91 98250 11223",
                    Role = "Owner",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(providerUser);
                await db.SaveChangesAsync();
            }

            // Memberships in both Aura and Glow (Deterministic GUIDs & Idempotency: Section 117)
            var memAuraId = Guid.Parse("31000000-0000-0000-0000-000000000001");
            var memGlowId = Guid.Parse("31000000-0000-0000-0000-000000000002");
            if (!await db.OrganizationMemberships.IgnoreQueryFilters().AnyAsync(m => m.Id == memAuraId))
            {
                db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Id = memAuraId,
                    TenantId = auraId,
                    UserId = providerUser.Id,
                    Role = "Owner",
                    JoinedAtUtc = DateTime.UtcNow
                });
            }
            if (!await db.OrganizationMemberships.IgnoreQueryFilters().AnyAsync(m => m.Id == memGlowId))
            {
                db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Id = memGlowId,
                    TenantId = glowId,
                    UserId = providerUser.Id,
                    Role = "Owner",
                    JoinedAtUtc = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();

            // 4. Seed Locations for Multi-Location Businesses across Ahmedabad, Mumbai, Rajkot, Surat, Bangalore (Section 115)
            var auraLoc1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
            var auraLoc2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
            var auraLoc3Id = Guid.Parse("40000000-0000-0000-0000-000000000003");
            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == auraLoc1Id))
            {
                db.Locations.Add(new Location
                {
                    Id = auraLoc1Id,
                    TenantId = auraId,
                    Name = "Bodakdev Flagship Sanctuary",
                    Address = "102 Sindhu Bhavan Marg, Bodakdev",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380054",
                    Latitude = 23.0396,
                    Longitude = 72.5074,
                    Phone = "+91 79 4001 8899",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == auraLoc2Id))
            {
                db.Locations.Add(new Location
                {
                    Id = auraLoc2Id,
                    TenantId = auraId,
                    Name = "Satellite Executive Branch",
                    Address = "45 Shyamal Cross Road, Satellite",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "380015",
                    Latitude = 23.0152,
                    Longitude = 72.5284,
                    Phone = "+91 79 4001 8877",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == auraLoc3Id))
            {
                db.Locations.Add(new Location
                {
                    Id = auraLoc3Id,
                    TenantId = auraId,
                    Name = "Aura BKC Executive Sanctuary",
                    Address = "G Block, Bandra Kurla Complex",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Country = "India",
                    PostalCode = "400051",
                    Latitude = 19.0660,
                    Longitude = 72.8680,
                    Phone = "+91 22 6601 8800",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            var mumbaiLoc1Id = Guid.Parse("40000000-0000-0000-0000-000000000031");
            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == mumbaiLoc1Id))
            {
                db.Locations.Add(new Location
                {
                    Id = mumbaiLoc1Id,
                    TenantId = mumbaiId,
                    Name = "Verdant Bandra West Sanctuary",
                    Address = "28 Waterfield Road, Bandra West",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Country = "India",
                    PostalCode = "400050",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    Phone = "+91 22 4901 8899",
                    Timezone = "Asia/Kolkata",
                    Currency = "INR",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            var vanguardLocId = Guid.Parse("40000000-0000-0000-0000-000000000041");
            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == vanguardLocId))
            {
                db.Locations.Add(new Location
                {
                    Id = vanguardLocId,
                    TenantId = vanguardId,
                    Name = "Vanguard Imperial Chambers",
                    Address = "502 Imperial Heights, Yagnik Road",
                    City = "Rajkot",
                    State = "Gujarat",
                    Country = "India",
                    PostalCode = "360001",
                    Latitude = 22.2980,
                    Longitude = 70.7960,
                    Phone = "+91 281 245 7711",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            var zenithLocBangaloreId = Guid.Parse("40000000-0000-0000-0000-000000000051");
            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == zenithLocBangaloreId))
            {
                db.Locations.Add(new Location
                {
                    Id = zenithLocBangaloreId,
                    TenantId = zenithId,
                    Name = "Zenith Indiranagar Flagship",
                    Address = "88 100 Feet Road, Indiranagar",
                    City = "Bangalore",
                    State = "Karnataka",
                    Country = "India",
                    PostalCode = "560038",
                    Latitude = 12.9784,
                    Longitude = 77.6408,
                    Phone = "+91 80 4123 9900",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            var zenithLocMumbaiId = Guid.Parse("40000000-0000-0000-0000-000000000052");
            if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == zenithLocMumbaiId))
            {
                db.Locations.Add(new Location
                {
                    Id = zenithLocMumbaiId,
                    TenantId = zenithId,
                    Name = "Zenith Bandra Reformer Sanctuary",
                    Address = "14 Linking Road, Bandra West",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Country = "India",
                    PostalCode = "400050",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    Phone = "+91 22 2640 1199",
                    Timezone = "Asia/Kolkata",
                    Currency = "USD",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();

            // 5. Seed Services for Aura Wellness
            var auraCatId = Guid.Parse("50000000-0000-0000-0000-000000000001");
            if (!await db.ServiceCategories.IgnoreQueryFilters().AnyAsync(c => c.Id == auraCatId))
            {
                var cat = new ServiceCategory
                {
                    Id = auraCatId,
                    TenantId = auraId,
                    Name = "Therapeutic Massage & Bodywork",
                    Description = "Holistic and restorative massage rituals"
                };
                db.ServiceCategories.Add(cat);

                var s1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");
                var s2Id = Guid.Parse("60000000-0000-0000-0000-000000000002");
                var s3Id = Guid.Parse("60000000-0000-0000-0000-000000000003");

                db.Services.AddRange(
                    new Service
                    {
                        Id = s1Id,
                        TenantId = auraId,
                        CategoryId = cat.Id,
                        Name = "Signature Aromatherapy Massage",
                        Description = "60-minute full body massage with hand-blended organic botanical oils.",
                        DurationMinutes = 60,
                        BufferBeforeMinutes = 10,
                        BufferAfterMinutes = 15,
                        Price = 75.00m,
                        Currency = "USD",
                        ColorHex = "#E8546A",
                        IsActive = true,
                        IsOnlineBookingEnabled = true
                    },
                    new Service
                    {
                        Id = s2Id,
                        TenantId = auraId,
                        CategoryId = cat.Id,
                        Name = "Deep Cleansing Hydrafacial",
                        Description = "Advanced vortex exfoliation, suction extraction, and intense peptide hydration.",
                        DurationMinutes = 45,
                        BufferBeforeMinutes = 5,
                        BufferAfterMinutes = 10,
                        Price = 95.00m,
                        Currency = "USD",
                        ColorHex = "#34D399",
                        IsActive = true,
                        IsOnlineBookingEnabled = true
                    },
                    new Service
                    {
                        Id = s3Id,
                        TenantId = auraId,
                        CategoryId = cat.Id,
                        Name = "Ayurvedic Shirodhara Ritual",
                        Description = "Meditative herbal oil flow across the forehead to soothe mental fatigue and anxiety.",
                        DurationMinutes = 50,
                        BufferBeforeMinutes = 10,
                        BufferAfterMinutes = 15,
                        Price = 80.00m,
                        Currency = "USD",
                        ColorHex = "#FBBF24",
                        IsActive = true,
                        IsOnlineBookingEnabled = true
                    }
                );
                await db.SaveChangesAsync();
            }

            // 6. Seed Staff for Aura Wellness
            var auraStaff1Id = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var auraStaff2Id = Guid.Parse("70000000-0000-0000-0000-000000000002");
            if (!await db.Staff.IgnoreQueryFilters().AnyAsync(s => s.Id == auraStaff1Id))
            {
                db.Staff.AddRange(
                    new Staff
                    {
                        Id = auraStaff1Id,
                        TenantId = auraId,
                        Name = "Dr. Priya Sharma",
                        Email = "priya.sharma@aura-wellness.local",
                        Phone = "+91 98251 33445",
                        Title = "Chief Aesthetician & Ayurvedic Lead",
                        Bio = "Over 12 years of clinical wellness leadership specializing in skin restoration and holistic bodywork.",
                        TimeZoneId = "Asia/Kolkata",
                        IsActive = true
                    },
                    new Staff
                    {
                        Id = auraStaff2Id,
                        TenantId = auraId,
                        Name = "Ananya Mehta",
                        Email = "ananya.mehta@aura-wellness.local",
                        Phone = "+91 98251 33446",
                        Title = "Senior Massage Therapist",
                        Bio = "Specialist in deep tissue, lymph drainage, and therapeutic aromatherapy.",
                        TimeZoneId = "Asia/Kolkata",
                        IsActive = true
                    }
                );
                await db.SaveChangesAsync();

                // Map staff to services
                var s1 = Guid.Parse("60000000-0000-0000-0000-000000000001");
                var s2 = Guid.Parse("60000000-0000-0000-0000-000000000002");
                var s3 = Guid.Parse("60000000-0000-0000-0000-000000000003");

                db.StaffServices.AddRange(
                    new StaffService { TenantId = auraId, StaffId = auraStaff1Id, ServiceId = s1 },
                    new StaffService { TenantId = auraId, StaffId = auraStaff1Id, ServiceId = s2 },
                    new StaffService { TenantId = auraId, StaffId = auraStaff2Id, ServiceId = s1 },
                    new StaffService { TenantId = auraId, StaffId = auraStaff2Id, ServiceId = s3 }
                );

                // Add working hours for staff
                for (int d = 1; d <= 6; d++)
                {
                    db.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = auraId,
                        StaffId = auraStaff1Id,
                        DayOfWeek = (DayOfWeek)d,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(18, 0)
                    });
                    db.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = auraId,
                        StaffId = auraStaff2Id,
                        DayOfWeek = (DayOfWeek)d,
                        StartTime = new TimeOnly(10, 0),
                        EndTime = new TimeOnly(19, 0)
                    });
                }
                await db.SaveChangesAsync();
            }

            // 7. Seed Product Categories & Products for Aura Wellness (Section 26)
            if (!await db.Products.IgnoreQueryFilters().AnyAsync(p => p.TenantId == auraId))
            {
                var catSkincareId = Guid.Parse("60000000-0000-0000-0000-000000000001");
                var catEquipmentId = Guid.Parse("60000000-0000-0000-0000-000000000002");

                var catSkincare = new ProductCategory
                {
                    Id = catSkincareId,
                    TenantId = auraId,
                    Name = "Skincare & Aromatherapy",
                    Slug = "skincare-aromatherapy",
                    Description = "Botanical formulations and essential extracts for skin rejuvenation.",
                    SortOrder = 1,
                    IsActive = true
                };

                var catEquipment = new ProductCategory
                {
                    Id = catEquipmentId,
                    TenantId = auraId,
                    Name = "Wellness Equipment & Accessories",
                    Slug = "wellness-equipment-accessories",
                    Description = "In-studio tools and home wellness equipment.",
                    SortOrder = 2,
                    IsActive = true
                };

                db.ProductCategories.AddRange(catSkincare, catEquipment);
                await db.SaveChangesAsync();

                var prod1Id = Guid.Parse("70000000-0000-0000-0000-000000000001");
                var prod1 = new Product
                {
                    Id = prod1Id,
                    TenantId = auraId,
                    CategoryId = catSkincare.Id,
                    Name = "Botanical Rosewater Facial Mist",
                    Slug = "botanical-rosewater-facial-mist",
                    Description = "Hydrosol distilled Bulgarian rose essence with hyaluronic acid to hydrate throughout the day.",
                    Price = 28.00m,
                    Currency = "USD",
                    Sku = "AURA-MIST-01",
                    StockQuantity = 35,
                    ReservedQuantity = 2,
                    SoldQuantity = 14,
                    LowStockThreshold = 5,
                    TrackInventory = true,
                    IsActive = true,
                    IsPurchasableOnline = true,
                    IsPublished = true,
                    ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80"
                };
                prod1.Images.Add(new ProductImage
                {
                    Id = Guid.Parse("71000000-0000-0000-0000-000000000001"),
                    TenantId = auraId,
                    ProductId = prod1.Id,
                    Url = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80",
                    AltText = "Botanical Rosewater Facial Mist bottle",
                    SortOrder = 0,
                    IsPrimary = true
                });

                var prod2 = new Product
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                    TenantId = auraId,
                    CategoryId = catSkincare.Id,
                    Name = "Organic French Lavender Essential Oil",
                    Slug = "organic-french-lavender-essential-oil",
                    Description = "Single-origin pure therapeutic lavender oil for evening relaxation and restorative sleep.",
                    Price = 34.00m,
                    Currency = "USD",
                    Sku = "AURA-OIL-02",
                    StockQuantity = 25,
                    ReservedQuantity = 1,
                    SoldQuantity = 22,
                    LowStockThreshold = 5,
                    TrackInventory = true,
                    IsActive = true,
                    IsPurchasableOnline = true,
                    IsPublished = true,
                    ImageUrl = "https://images.unsplash.com/photo-1608248597359-24755f190696?auto=format&fit=crop&w=600&q=80"
                };

                // State 2: Active but out of stock
                var prodOutOfStock = new Product
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                    TenantId = auraId,
                    CategoryId = catEquipment.Id,
                    Name = "Silk Acoustic Recovery Eye Pillow",
                    Slug = "silk-acoustic-recovery-eye-pillow",
                    Description = "Mulberry silk weighted eye pillow filled with organic lavender seeds. Promotes parasympathetic activation.",
                    Price = 38.00m,
                    Currency = "USD",
                    Sku = "AURA-EYE-03",
                    StockQuantity = 0,
                    ReservedQuantity = 0,
                    SoldQuantity = 50,
                    LowStockThreshold = 5,
                    TrackInventory = true,
                    IsActive = true,
                    IsPurchasableOnline = true,
                    IsPublished = true,
                    ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=600&q=80"
                };

                // State 3: Inactive / Archived
                var prodInactive = new Product
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000004"),
                    TenantId = auraId,
                    CategoryId = catSkincare.Id,
                    Name = "Discontinued Eucalyptus Bath Salts",
                    Slug = "discontinued-eucalyptus-bath-salts",
                    Description = "Previous season mineral soak blend. Discontinued from active catalog.",
                    Price = 22.00m,
                    Currency = "USD",
                    Sku = "AURA-BTH-04",
                    StockQuantity = 0,
                    ReservedQuantity = 0,
                    SoldQuantity = 40,
                    TrackInventory = true,
                    IsActive = false,
                    IsPurchasableOnline = false,
                    IsPublished = false
                };

                // State 4: Not available for online purchase (In-Store Only)
                var prodStoreOnly = new Product
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000005"),
                    TenantId = auraId,
                    CategoryId = catEquipment.Id,
                    Name = "Professional Basalt Hot Stone Therapy Set",
                    Slug = "professional-basalt-hot-stone-set",
                    Description = "In-studio certified volcanic basalt stones. Exclusively available for local in-person studio pickup.",
                    Price = 145.00m,
                    Currency = "USD",
                    Sku = "AURA-STN-05",
                    StockQuantity = 8,
                    ReservedQuantity = 0,
                    SoldQuantity = 12,
                    LowStockThreshold = 2,
                    TrackInventory = true,
                    IsActive = true,
                    IsPurchasableOnline = false,
                    IsPublished = true,
                    ImageUrl = "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?auto=format&fit=crop&w=600&q=80"
                };

                // State 5: Unpublished (Draft)
                var prodDraft = new Product
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000006"),
                    TenantId = auraId,
                    CategoryId = catSkincare.Id,
                    Name = "Upcoming Peptide Radiance Firming Serum",
                    Slug = "upcoming-peptide-radiance-firming-serum",
                    Description = "Advanced copper peptide and snow mushroom serum in pre-launch preparation.",
                    Price = 68.00m,
                    Currency = "USD",
                    Sku = "AURA-SRM-06",
                    StockQuantity = 50,
                    ReservedQuantity = 0,
                    SoldQuantity = 0,
                    LowStockThreshold = 10,
                    TrackInventory = true,
                    IsActive = true,
                    IsPurchasableOnline = true,
                    IsPublished = false,
                    ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=600&q=80"
                };

                db.Products.AddRange(prod1, prod2, prodOutOfStock, prodInactive, prodStoreOnly, prodDraft);
                await db.SaveChangesAsync();
            }

            // 8. Seed Customer Bookings & Orders for customer@bookline.local
            if (customerUser != null)
            {
                var existingBookings = await db.Bookings.IgnoreQueryFilters()
                    .AnyAsync(b => b.CustomerId == customerUser.Id);

                if (!existingBookings)
                {
                    var s1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");

                    // 1 upcoming confirmed booking (Deterministic GUID: Section 117)
                    var bUpcomingId = Guid.Parse("80000000-0000-0000-0000-000000000001");
                    var upcoming = new Booking
                    {
                        Id = bUpcomingId,
                        TenantId = auraId,
                        StaffId = auraStaff1Id,
                        ServiceId = s1Id,
                        CustomerId = customerUser.Id,
                        LocationId = auraLoc1Id,
                        BookingReference = "BL-749218",
                        StartUtc = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10),
                        EndUtc = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(11),
                        TotalPrice = 75.00m,
                        DepositPaid = 25.00m,
                        CustomerNotes = "Looking forward to relaxing session."
                    };
                    upcoming.Confirm();
                    db.Bookings.Add(upcoming);

                    // 1 past completed booking
                    var bPastId = Guid.Parse("80000000-0000-0000-0000-000000000002");
                    var past = new Booking
                    {
                        Id = bPastId,
                        TenantId = auraId,
                        StaffId = auraStaff1Id,
                        ServiceId = s1Id,
                        CustomerId = customerUser.Id,
                        LocationId = auraLoc1Id,
                        BookingReference = "BL-512034",
                        StartUtc = DateTimeOffset.UtcNow.AddDays(-5).Date.AddHours(14),
                        EndUtc = DateTimeOffset.UtcNow.AddDays(-5).Date.AddHours(15),
                        TotalPrice = 75.00m,
                        DepositPaid = 25.00m
                    };
                    past.Confirm();
                    past.Complete();
                    db.Bookings.Add(past);

                    // 1 pending booking (Section 125: mixed states)
                    var bPendingId = Guid.Parse("80000000-0000-0000-0000-000000000003");
                    var pendingBooking = new Booking
                    {
                        Id = bPendingId,
                        TenantId = auraId,
                        StaffId = auraStaff1Id,
                        ServiceId = s1Id,
                        CustomerId = customerUser.Id,
                        LocationId = auraLoc1Id,
                        BookingReference = "BL-991201",
                        StartUtc = DateTimeOffset.UtcNow.AddDays(5).Date.AddHours(11),
                        EndUtc = DateTimeOffset.UtcNow.AddDays(5).Date.AddHours(12),
                        TotalPrice = 75.00m,
                        DepositPaid = 0m,
                        CustomerNotes = "Pending provider confirmation."
                    };
                    db.Bookings.Add(pendingBooking);

                    // 1 cancelled booking (Section 125: mixed states)
                    var bCancelledId = Guid.Parse("80000000-0000-0000-0000-000000000004");
                    var cancelledBooking = new Booking
                    {
                        Id = bCancelledId,
                        TenantId = auraId,
                        StaffId = auraStaff1Id,
                        ServiceId = s1Id,
                        CustomerId = customerUser.Id,
                        LocationId = auraLoc1Id,
                        BookingReference = "BL-310492",
                        StartUtc = DateTimeOffset.UtcNow.AddDays(-2).Date.AddHours(16),
                        EndUtc = DateTimeOffset.UtcNow.AddDays(-2).Date.AddHours(17),
                        TotalPrice = 75.00m,
                        DepositPaid = 25.00m
                    };
                    cancelledBooking.Cancel("Client requested cancellation due to travel conflict", "Jane Customer");
                    db.Bookings.Add(cancelledBooking);

                    // Favorite provider
                    db.Favorites.Add(new Favorite
                    {
                        Id = Guid.Parse("81000000-0000-0000-0000-000000000001"),
                        CustomerId = customerUser.Id,
                        TenantId = auraId,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    // Customer review (Clearly marked as demo seeded record: Section 125)
                    db.Reviews.Add(new Review
                    {
                        Id = Guid.Parse("82000000-0000-0000-0000-000000000001"),
                        TenantId = auraId,
                        CustomerId = customerUser.Id,
                        BookingId = past.Id,
                        CustomerName = "Jane Customer [Demo Seeded]",
                        Rating = 5,
                        Title = "[Demo Seeded] Exceptional sanctuary & therapist",
                        Comment = "Dr. Priya was attentive and the bespoke oil blend smelled divine. The ambient lighting and calm atmosphere is unmatched in Ahmedabad.",
                        ProviderResponse = "Thank you so much Jane! It was our pleasure to host you at our Bodakdev flagship.",
                        ModerationStatus = ModerationStatus.Approved,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-4),
                        RespondedAtUtc = DateTime.UtcNow.AddDays(-3)
                    });

                    // Retail orders in mixed states (Section 125)
                    // 1. Processing order
                    var orderProcessing = new Order
                    {
                        Id = Guid.Parse("83000000-0000-0000-0000-000000000001"),
                        TenantId = auraId,
                        CustomerId = customerUser.Id,
                        OrderNumber = "ORD-839210",
                        CustomerName = "Jane Customer",
                        CustomerEmail = customerEmail,
                        CustomerPhone = "+91 98765 43210",
                        ShippingAddress = "12 Gulmohar Park, Satellite, Ahmedabad 380015",
                        Subtotal = 62.00m,
                        Tax = 3.10m,
                        TotalAmount = 65.10m,
                        Currency = "USD",
                        Status = OrderStatus.Processing,
                        PaidAtUtc = DateTime.UtcNow.AddDays(-1),
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                    };
                    db.Orders.Add(orderProcessing);

                    // 2. Delivered order
                    var orderDelivered = new Order
                    {
                        Id = Guid.Parse("83000000-0000-0000-0000-000000000002"),
                        TenantId = auraId,
                        CustomerId = customerUser.Id,
                        OrderNumber = "ORD-771234",
                        CustomerName = "Jane Customer",
                        CustomerEmail = customerEmail,
                        CustomerPhone = "+91 98765 43210",
                        ShippingAddress = "12 Gulmohar Park, Satellite, Ahmedabad 380015",
                        Subtotal = 34.00m,
                        Tax = 1.70m,
                        TotalAmount = 35.70m,
                        Currency = "USD",
                        Status = OrderStatus.Completed,
                        PaidAtUtc = DateTime.UtcNow.AddDays(-10),
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                    };
                    db.Orders.Add(orderDelivered);

                    // 3. Cancelled order
                    var orderCancelled = new Order
                    {
                        Id = Guid.Parse("83000000-0000-0000-0000-000000000003"),
                        TenantId = auraId,
                        CustomerId = customerUser.Id,
                        OrderNumber = "ORD-112233",
                        CustomerName = "Jane Customer",
                        CustomerEmail = customerEmail,
                        CustomerPhone = "+91 98765 43210",
                        ShippingAddress = "12 Gulmohar Park, Satellite, Ahmedabad 380015",
                        Subtotal = 28.00m,
                        Tax = 1.40m,
                        TotalAmount = 29.40m,
                        Currency = "USD",
                        Status = OrderStatus.Cancelled,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
                    };
                    db.Orders.Add(orderCancelled);

                    await db.SaveChangesAsync();
                }
            }

            // 9. Deterministic Demo Environment for Glow Studio (Phase 42 & Phase 43)
            await SeedGlowStudioDemoEnvironmentAsync(db, passwordHasher, logger);

            logger.LogInformation("MarketplaceDbSeeder successfully populated multi-vendor marketplace data.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during MarketplaceDbSeeder execution.");
        }
    }

    /// <summary>
    /// Phase 42 & 43: Create deterministic demo environment for Glow Studio.
    /// Locations: Ahmedabad, Surat
    /// Staff: multiple realistic staff
    /// Services: Haircut, Haircut & Style, Full Colour, Facial, Massage, Consultation, Keratin Treatment
    /// Working hours, breaks, time off, appointments (free, held, booked, completed, cancelled, no-show),
    /// customers, notifications, audit events, and demo account (demo@bookline.local / BooklineDemo123!).
    /// </summary>
    private static async Task SeedGlowStudioDemoEnvironmentAsync(BooklineDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        var glowId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var glowTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == glowId);
        if (glowTenant != null)
        {
            glowTenant.Name = "Glow Studio";
            glowTenant.Slug = "glow-studio";
            glowTenant.Currency = "INR";
            glowTenant.City = "Ahmedabad";
            glowTenant.State = "Gujarat";
            glowTenant.Country = "India";
            glowTenant.TimeZoneId = "Asia/Kolkata";
            glowTenant.IsPublished = true;
            glowTenant.IsActive = true;
            await db.SaveChangesAsync();
        }

        // 1. Phase 43: Demo Account (demo@bookline.local / BooklineDemo123!)
        var demoEmail = "demo@bookline.local";
        var demoUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == demoEmail);
        if (demoUser == null)
        {
            demoUser = new AppUser
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                TenantId = glowId,
                Email = demoEmail,
                PasswordHash = passwordHasher.HashPassword("BooklineDemo123!"),
                FirstName = "Demo",
                LastName = "Owner",
                Role = "Owner",
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Users.Add(demoUser);
            await db.SaveChangesAsync();
        }
        else if (demoUser.TenantId != glowId)
        {
            demoUser.TenantId = glowId;
            demoUser.Role = "Owner";
            await db.SaveChangesAsync();
        }

        // Ensure OrganizationMembership for demo user at Glow Studio
        var membership = await db.OrganizationMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == demoUser.Id && m.TenantId == glowId);
        if (membership == null)
        {
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                Id = Guid.Parse("31000000-0000-0000-0000-000000000003"),
                TenantId = glowId,
                UserId = demoUser.Id,
                Role = "Owner",
                JoinedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 2. Locations: Ahmedabad and Surat (Phase 42)
        var locAhmedabadId = Guid.Parse("40000000-0000-0000-0000-000000000011");
        var locSuratId = Guid.Parse("40000000-0000-0000-0000-000000000012");

        if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == locAhmedabadId))
        {
            db.Locations.Add(new Location
            {
                Id = locAhmedabadId,
                TenantId = glowId,
                Name = "Glow Studio · Bodakdev Flagship",
                Address = "Sindhu Bhavan Road, Bodakdev",
                City = "Ahmedabad",
                State = "Gujarat",
                Country = "India",
                PostalCode = "380054",
                Latitude = 23.0396,
                Longitude = 72.5074,
                Phone = "+91 79 4001 9900",
                Timezone = "Asia/Kolkata",
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.Locations.IgnoreQueryFilters().AnyAsync(l => l.Id == locSuratId))
        {
            db.Locations.Add(new Location
            {
                Id = locSuratId,
                TenantId = glowId,
                Name = "Glow Studio · Vesu Studio",
                Address = "Dumas Road, Vesu",
                City = "Surat",
                State = "Gujarat",
                Country = "India",
                PostalCode = "395007",
                Latitude = 21.1415,
                Longitude = 72.7758,
                Phone = "+91 261 4001 9911",
                Timezone = "Asia/Kolkata",
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // 3. Staff: multiple realistic staff (Phase 42)
        var staffPriyaId = Guid.Parse("70000000-0000-0000-0000-000000000011");
        var staffAaravId = Guid.Parse("70000000-0000-0000-0000-000000000012");
        var staffDiyaId = Guid.Parse("70000000-0000-0000-0000-000000000013");
        var staffRohanId = Guid.Parse("70000000-0000-0000-0000-000000000014");

        if (!await db.Staff.IgnoreQueryFilters().AnyAsync(s => s.Id == staffPriyaId))
        {
            db.Staff.AddRange(
                new Staff
                {
                    Id = staffPriyaId,
                    TenantId = glowId,
                    Name = "Priya Sharma",
                    Email = "priya.sharma@glowstudio.local",
                    Phone = "+91 98250 88771",
                    Title = "Master Stylist & Lead Colorist",
                    Bio = "Specializing in precision hair cutting, bespoke balayage, and restorative keratin hair therapies.",
                    TimeZoneId = "Asia/Kolkata",
                    IsActive = true
                },
                new Staff
                {
                    Id = staffAaravId,
                    TenantId = glowId,
                    Name = "Aarav Mehta",
                    Email = "aarav.mehta@glowstudio.local",
                    Phone = "+91 98250 88772",
                    Title = "Senior Barber & Hair Stylist",
                    Bio = "Master of contemporary fades, beard architecture, and classic men's grooming rituals.",
                    TimeZoneId = "Asia/Kolkata",
                    IsActive = true
                },
                new Staff
                {
                    Id = staffDiyaId,
                    TenantId = glowId,
                    Name = "Diya Patel",
                    Email = "diya.patel@glowstudio.local",
                    Phone = "+91 98250 88773",
                    Title = "Esthetician & Skincare Specialist",
                    Bio = "Certified clinical aesthetician focusing on deep-pore facials and organic dermal rejuvenation.",
                    TimeZoneId = "Asia/Kolkata",
                    IsActive = true
                },
                new Staff
                {
                    Id = staffRohanId,
                    TenantId = glowId,
                    Name = "Rohan Shah",
                    Email = "rohan.shah@glowstudio.local",
                    Phone = "+91 98250 88774",
                    Title = "Therapeutic Massage Specialist",
                    Bio = "Expert in deep tissue, reflexology, and therapeutic scalp and shoulder relief massage.",
                    TimeZoneId = "Asia/Kolkata",
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();

            // 4. Working Hours and Breaks (Phase 42)
            // Mon - Sat: Shift 1 (09:00 - 13:00), Shift 2 (14:00 - 19:00) -> 13:00 - 14:00 Lunch Break!
            var staffIds = new[] { staffPriyaId, staffAaravId, staffDiyaId, staffRohanId };
            foreach (var sId in staffIds)
            {
                for (int day = 1; day <= 6; day++)
                {
                    db.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = glowId,
                        StaffId = sId,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(13, 0),
                        Label = "Morning Shift"
                    });
                    db.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = glowId,
                        StaffId = sId,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeOnly(14, 0),
                        EndTime = new TimeOnly(19, 0),
                        Label = "Afternoon Shift"
                    });
                }
            }
            await db.SaveChangesAsync();

            // 5. Time Off (Deterministic GUIDs: Section 117)
            db.TimeOffs.AddRange(
                new TimeOff
                {
                    Id = Guid.Parse("84000000-0000-0000-0000-000000000001"),
                    TenantId = glowId,
                    StaffId = staffDiyaId,
                    StartUtc = DateTimeOffset.UtcNow.AddDays(7).Date.AddHours(9),
                    EndUtc = DateTimeOffset.UtcNow.AddDays(8).Date.AddHours(19),
                    Type = "Vacation",
                    Reason = "Annual leave"
                },
                new TimeOff
                {
                    Id = Guid.Parse("84000000-0000-0000-0000-000000000002"),
                    TenantId = glowId,
                    StaffId = staffAaravId,
                    StartUtc = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(9),
                    EndUtc = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(19),
                    Type = "Holiday",
                    Reason = "Advanced Barbering Masterclass"
                }
            );
            await db.SaveChangesAsync();
        }

        // 6. Services: Exact 7 required services (Phase 42)
        var srvHaircutId = Guid.Parse("60000000-0000-0000-0000-000000000011");
        var srvHaircutStyleId = Guid.Parse("60000000-0000-0000-0000-000000000012");
        var srvFullColourId = Guid.Parse("60000000-0000-0000-0000-000000000013");
        var srvFacialId = Guid.Parse("60000000-0000-0000-0000-000000000014");
        var srvMassageId = Guid.Parse("60000000-0000-0000-0000-000000000015");
        var srvConsultationId = Guid.Parse("60000000-0000-0000-0000-000000000016");
        var srvKeratinId = Guid.Parse("60000000-0000-0000-0000-000000000017");

        if (!await db.Services.IgnoreQueryFilters().AnyAsync(s => s.Id == srvHaircutId))
        {
            var catHair = new ServiceCategory
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000011"),
                TenantId = glowId,
                Name = "Hair Artistry & Barbering",
                Description = "Custom haircuts, precision fades, and styling services"
            };
            var catWellness = new ServiceCategory
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000012"),
                TenantId = glowId,
                Name = "Skin, Scalp & Wellness",
                Description = "Clinical facials, therapeutic massage, and hair treatments"
            };
            db.ServiceCategories.AddRange(catHair, catWellness);
            await db.SaveChangesAsync();

            db.Services.AddRange(
                new Service
                {
                    Id = srvHaircutId,
                    TenantId = glowId,
                    CategoryId = catHair.Id,
                    Name = "Haircut",
                    Slug = "haircut",
                    Description = "Tailored precision cut and wash with styling consultation.",
                    DurationMinutes = 30,
                    BufferBeforeMinutes = 0,
                    BufferAfterMinutes = 5,
                    Price = 600.00m,
                    Currency = "INR",
                    ColorHex = "#E8546A",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvHaircutStyleId,
                    TenantId = glowId,
                    CategoryId = catHair.Id,
                    Name = "Haircut & Style",
                    Slug = "haircut-and-style",
                    Description = "Full precision haircut, blow dry, hot tool styling, and finishing treatment.",
                    DurationMinutes = 45,
                    BufferBeforeMinutes = 5,
                    BufferAfterMinutes = 15,
                    Price = 1200.00m,
                    Currency = "INR",
                    ColorHex = "#F43F5E",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvFullColourId,
                    TenantId = glowId,
                    CategoryId = catHair.Id,
                    Name = "Full Colour",
                    Slug = "full-colour",
                    Description = "Full single-process global color application using ammonia-free Italian pigments.",
                    DurationMinutes = 90,
                    BufferBeforeMinutes = 5,
                    BufferAfterMinutes = 15,
                    Price = 3500.00m,
                    Currency = "INR",
                    ColorHex = "#A855F7",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvFacialId,
                    TenantId = glowId,
                    CategoryId = catWellness.Id,
                    Name = "Facial",
                    Slug = "facial",
                    Description = "Restorative facial with custom botanical enzymes, steaming, and hydration mask.",
                    DurationMinutes = 60,
                    BufferBeforeMinutes = 5,
                    BufferAfterMinutes = 10,
                    Price = 2200.00m,
                    Currency = "INR",
                    ColorHex = "#34D399",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvMassageId,
                    TenantId = glowId,
                    CategoryId = catWellness.Id,
                    Name = "Massage",
                    Slug = "massage",
                    Description = "Therapeutic back, neck, and shoulder acupressure massage.",
                    DurationMinutes = 60,
                    BufferBeforeMinutes = 5,
                    BufferAfterMinutes = 15,
                    Price = 2500.00m,
                    Currency = "INR",
                    ColorHex = "#38BDF8",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvConsultationId,
                    TenantId = glowId,
                    CategoryId = catHair.Id,
                    Name = "Consultation",
                    Slug = "consultation",
                    Description = "One-on-one specialist consultation for color correction, bridal, or scalp health.",
                    DurationMinutes = 15,
                    BufferBeforeMinutes = 0,
                    BufferAfterMinutes = 5,
                    Price = 300.00m,
                    Currency = "INR",
                    ColorHex = "#FBBF24",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                },
                new Service
                {
                    Id = srvKeratinId,
                    TenantId = glowId,
                    CategoryId = catHair.Id,
                    Name = "Keratin Treatment",
                    Slug = "keratin-treatment",
                    Description = "Formaldehyde-free smoothing treatment infused with pure keratin and argan oil.",
                    DurationMinutes = 120,
                    BufferBeforeMinutes = 10,
                    BufferAfterMinutes = 20,
                    Price = 4800.00m,
                    Currency = "INR",
                    ColorHex = "#EC4899",
                    IsActive = true,
                    IsOnlineBookingEnabled = true,
                    IsPublished = true
                }
            );
            await db.SaveChangesAsync();

            // Link Staff to Services
            db.StaffServices.AddRange(
                new StaffService { TenantId = glowId, StaffId = staffPriyaId, ServiceId = srvHaircutId },
                new StaffService { TenantId = glowId, StaffId = staffPriyaId, ServiceId = srvHaircutStyleId },
                new StaffService { TenantId = glowId, StaffId = staffPriyaId, ServiceId = srvFullColourId },
                new StaffService { TenantId = glowId, StaffId = staffPriyaId, ServiceId = srvConsultationId },
                new StaffService { TenantId = glowId, StaffId = staffPriyaId, ServiceId = srvKeratinId },

                new StaffService { TenantId = glowId, StaffId = staffAaravId, ServiceId = srvHaircutId },
                new StaffService { TenantId = glowId, StaffId = staffAaravId, ServiceId = srvHaircutStyleId },
                new StaffService { TenantId = glowId, StaffId = staffAaravId, ServiceId = srvConsultationId },

                new StaffService { TenantId = glowId, StaffId = staffDiyaId, ServiceId = srvFacialId },
                new StaffService { TenantId = glowId, StaffId = staffDiyaId, ServiceId = srvConsultationId },

                new StaffService { TenantId = glowId, StaffId = staffRohanId, ServiceId = srvMassageId },
                new StaffService { TenantId = glowId, StaffId = staffRohanId, ServiceId = srvConsultationId }
            );

            // Link Services to Locations (Ahmedabad & Surat)
            var allSrvIds = new[] { srvHaircutId, srvHaircutStyleId, srvFullColourId, srvFacialId, srvMassageId, srvConsultationId, srvKeratinId };
            foreach (var sId in allSrvIds)
            {
                db.ServiceLocations.Add(new ServiceLocation { LocationId = locAhmedabadId, ServiceId = sId });
                db.ServiceLocations.Add(new ServiceLocation { LocationId = locSuratId, ServiceId = sId });
            }
            await db.SaveChangesAsync();
        }

        // 7. Customers (Phase 42)
        var custNehaId = Guid.Parse("20000000-0000-0000-0000-000000000011");
        var custAmitId = Guid.Parse("20000000-0000-0000-0000-000000000012");
        var custSunitaId = Guid.Parse("20000000-0000-0000-0000-000000000013");
        var custVikramId = Guid.Parse("20000000-0000-0000-0000-000000000014");

        if (!await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Id == custNehaId))
        {
            db.Customers.AddRange(
                new Customer
                {
                    Id = custNehaId,
                    TenantId = glowId,
                    FirstName = "Neha",
                    LastName = "Desai",
                    Email = "neha.desai@example.com",
                    Phone = "+91 98240 11221",
                    Notes = "Prefers organic hair products. Allergic to strong ammonia scents."
                },
                new Customer
                {
                    Id = custAmitId,
                    TenantId = glowId,
                    FirstName = "Amit",
                    LastName = "Trivedi",
                    Email = "amit.trivedi@example.com",
                    Phone = "+91 98240 11222",
                    Notes = "Regular weekend client for precision fade and styling."
                },
                new Customer
                {
                    Id = custSunitaId,
                    TenantId = glowId,
                    FirstName = "Sunita",
                    LastName = "Rao",
                    Email = "sunita.rao@example.com",
                    Phone = "+91 98240 11223"
                },
                new Customer
                {
                    Id = custVikramId,
                    TenantId = glowId,
                    FirstName = "Vikram",
                    LastName = "Joshi",
                    Email = "vikram.joshi@example.com",
                    Phone = "+91 98240 11224"
                }
            );
            await db.SaveChangesAsync();
        }

        // 8. Appointments in MIX of states: free, held, booked, completed, cancelled, no-show (Phase 42)
        if (!await db.Bookings.IgnoreQueryFilters().AnyAsync(b => b.TenantId == glowId))
        {
            var baseDate = DateTimeOffset.UtcNow.Date;

            // State A: Booked / Confirmed
            var bConfirmedId = Guid.Parse("85000000-0000-0000-0000-000000000001");
            var bConfirmed = new Booking
            {
                Id = bConfirmedId,
                TenantId = glowId,
                StaffId = staffPriyaId,
                ServiceId = srvHaircutId,
                CustomerId = custNehaId,
                LocationId = locAhmedabadId,
                BookingReference = "BL-GLW-101",
                StartUtc = baseDate.AddDays(1).AddHours(10), // Tomorrow 10:00 AM
                EndUtc = baseDate.AddDays(1).AddHours(10).AddMinutes(30),
                TotalPrice = 600.00m,
                DepositPaid = 200.00m,
                CustomerNotes = "First time at Glow Studio Ahmedabad."
            };
            bConfirmed.Confirm();
            db.Bookings.Add(bConfirmed);

            // State B: Completed
            var bCompletedId = Guid.Parse("85000000-0000-0000-0000-000000000002");
            var bCompleted = new Booking
            {
                Id = bCompletedId,
                TenantId = glowId,
                StaffId = staffAaravId,
                ServiceId = srvHaircutStyleId,
                CustomerId = custAmitId,
                LocationId = locAhmedabadId,
                BookingReference = "BL-GLW-102",
                StartUtc = baseDate.AddDays(-1).AddHours(15), // Yesterday 3:00 PM
                EndUtc = baseDate.AddDays(-1).AddHours(15).AddMinutes(45),
                TotalPrice = 1200.00m,
                DepositPaid = 1200.00m,
                CustomerNotes = "Haircut & Style completed."
            };
            bCompleted.Confirm();
            bCompleted.Complete();
            db.Bookings.Add(bCompleted);

            // State C: Cancelled
            var bCancelledId = Guid.Parse("85000000-0000-0000-0000-000000000003");
            var bCancelled = new Booking
            {
                Id = bCancelledId,
                TenantId = glowId,
                StaffId = staffPriyaId,
                ServiceId = srvFullColourId,
                CustomerId = custSunitaId,
                LocationId = locAhmedabadId,
                BookingReference = "BL-GLW-103",
                StartUtc = baseDate.AddDays(-2).AddHours(11),
                EndUtc = baseDate.AddDays(-2).AddHours(12).AddMinutes(30),
                TotalPrice = 3500.00m,
                DepositPaid = 500.00m
            };
            bCancelled.Cancel("Client rescheduled due to illness", "Sunita Rao");
            db.Bookings.Add(bCancelled);

            // State D: No-Show
            var bNoShowId = Guid.Parse("85000000-0000-0000-0000-000000000004");
            var bNoShow = new Booking
            {
                Id = bNoShowId,
                TenantId = glowId,
                StaffId = staffPriyaId,
                ServiceId = srvKeratinId,
                CustomerId = custVikramId,
                LocationId = locAhmedabadId,
                BookingReference = "BL-GLW-104",
                StartUtc = baseDate.AddDays(-3).AddHours(16),
                EndUtc = baseDate.AddDays(-3).AddHours(18),
                TotalPrice = 4800.00m,
                DepositPaid = 1000.00m
            };
            bNoShow.Confirm();
            bNoShow.MarkNoShow();
            db.Bookings.Add(bNoShow);

            // State E: Held (BookingHold)
            db.BookingHolds.Add(new BookingHold
            {
                Id = Guid.Parse("86000000-0000-0000-0000-000000000001"),
                TenantId = glowId,
                LocationId = locAhmedabadId,
                StaffId = staffPriyaId,
                ServiceId = srvHaircutId,
                StartUtc = baseDate.AddDays(1).AddHours(11),
                EndUtc = baseDate.AddDays(1).AddHours(11).AddMinutes(30),
                HoldToken = "HOLD-GLW-DEMO-01",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                IsReleased = false
            });

            // State F: Free slots are determined by the slot engine based on working hours minus bookings & holds.

            // 9. Notifications (Phase 42 - Deterministic GUIDs)
            db.NotificationLogs.AddRange(
                new NotificationLog
                {
                    Id = Guid.Parse("87000000-0000-0000-0000-000000000001"),
                    TenantId = glowId,
                    BookingId = bConfirmed.Id,
                    CustomerId = custNehaId,
                    RecipientEmail = "neha.desai@example.com",
                    RecipientPhone = "+91 98240 11221",
                    NotificationType = NotificationType.BookingConfirmation,
                    Channel = NotificationChannel.Email,
                    Status = NotificationStatus.Sent,
                    Subject = "Appointment Confirmed: Haircut at Glow Studio",
                    Body = "Your haircut with Priya Sharma is confirmed for tomorrow at 10:00 AM.",
                    SentAtUtc = DateTime.UtcNow.AddHours(-2),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
                },
                new NotificationLog
                {
                    Id = Guid.Parse("87000000-0000-0000-0000-000000000002"),
                    TenantId = glowId,
                    BookingId = bConfirmed.Id,
                    CustomerId = custNehaId,
                    RecipientEmail = "neha.desai@example.com",
                    RecipientPhone = "+91 98240 11221",
                    NotificationType = NotificationType.Reminder24h,
                    Channel = NotificationChannel.SMS,
                    Status = NotificationStatus.Sent,
                    Subject = "Reminder: Upcoming session tomorrow at Glow Studio",
                    Body = "Reminder: Your appointment at Glow Studio Bodakdev is tomorrow at 10:00 AM. Please arrive 5 minutes early.",
                    SentAtUtc = DateTime.UtcNow.AddMinutes(-30),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30)
                },
                new NotificationLog
                {
                    Id = Guid.Parse("87000000-0000-0000-0000-000000000003"),
                    TenantId = glowId,
                    BookingId = bCancelled.Id,
                    CustomerId = custSunitaId,
                    RecipientEmail = "sunita.rao@example.com",
                    NotificationType = NotificationType.BookingCancellation,
                    Channel = NotificationChannel.Email,
                    Status = NotificationStatus.Sent,
                    Subject = "Booking Cancelled: Full Colour at Glow Studio",
                    Body = "Your booking BL-GLW-103 has been cancelled upon your request.",
                    SentAtUtc = DateTime.UtcNow.AddDays(-2),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                }
            );

            // 10. Audit Events (Phase 42 - Deterministic GUIDs)
            db.AuditLogs.AddRange(
                new AuditLog
                {
                    Id = Guid.Parse("88000000-0000-0000-0000-000000000001"),
                    TenantId = glowId,
                    Actor = "demo@bookline.local",
                    Action = "TenantCreated",
                    Target = "Glow Studio",
                    MetadataJson = "{\"slug\":\"glow-studio\",\"city\":\"Ahmedabad\"}",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-30)
                },
                new AuditLog
                {
                    Id = Guid.Parse("88000000-0000-0000-0000-000000000002"),
                    TenantId = glowId,
                    Actor = "demo@bookline.local",
                    Action = "LocationAdded",
                    Target = "Glow Studio · Bodakdev Flagship",
                    MetadataJson = "{\"city\":\"Ahmedabad\",\"timezone\":\"Asia/Kolkata\"}",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-28)
                },
                new AuditLog
                {
                    Id = Guid.Parse("88000000-0000-0000-0000-000000000003"),
                    TenantId = glowId,
                    Actor = "demo@bookline.local",
                    Action = "StaffOnboarded",
                    Target = "Priya Sharma",
                    MetadataJson = "{\"title\":\"Master Stylist\"}",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-20)
                },
                new AuditLog
                {
                    Id = Guid.Parse("88000000-0000-0000-0000-000000000004"),
                    TenantId = glowId,
                    Actor = "demo@bookline.local",
                    Action = "ServiceCatalogUpdated",
                    Target = "7 Services Added",
                    MetadataJson = "{\"servicesCount\":7}",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-15)
                },
                new AuditLog
                {
                    Id = Guid.Parse("88000000-0000-0000-0000-000000000005"),
                    TenantId = glowId,
                    Actor = "demo@bookline.local",
                    Action = "BookingStatusChanged",
                    Target = "BL-GLW-101",
                    MetadataJson = "{\"status\":\"Confirmed\"}",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2)
                }
            );

            await db.SaveChangesAsync();
        }

        logger.LogInformation("Glow Studio deterministic demo environment seeded successfully.");
    }
}
