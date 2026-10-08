using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookline.Infrastructure.Persistence.Seed;

public static class MarketplaceDbSeeder
{
    public static async Task SeedAsync(BooklineDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        try
        {
            // 1. Seed Demo Accounts
            var adminEmail = "admin@bookline.local";
            var providerEmail = "provider@bookline.local";
            var customerEmail = "customer@bookline.local";

            var adminUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    TenantId = Guid.Empty,
                    Email = adminEmail,
                    PasswordHash = passwordHasher.HashPassword("Admin123!"),
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
                    PasswordHash = passwordHasher.HashPassword("Customer123!"),
                    FirstName = "Jane",
                    LastName = "Customer",
                    Phone = "+91 98765 43210",
                    Role = "Customer",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(customerUser);
            }

            // 2. Seed Multi-Vendor Providers
            var auraId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            var glowId = Guid.Parse("30000000-0000-0000-0000-000000000002");
            var apexId = Guid.Parse("30000000-0000-0000-0000-000000000003");
            var zenithId = Guid.Parse("30000000-0000-0000-0000-000000000004");
            var luminaId = Guid.Parse("30000000-0000-0000-0000-000000000005");
            var vanguardId = Guid.Parse("30000000-0000-0000-0000-000000000006");

            var auraTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == auraId);
            if (auraTenant == null)
            {
                auraTenant = new Tenant
                {
                    Id = auraId,
                    Name = "Aura Wellness & Spa",
                    Slug = "aura-wellness",
                    Category = "Beauty & Wellness",
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
                    Name = "Glow Hair & Beauty Lounge",
                    Slug = "glow-lounge",
                    Category = "Beauty & Wellness",
                    BusinessType = "Editorial Hair Studio",
                    Description = "High-fashion hair artistry, balayage, and clinical keratin hair care by international master stylists.",
                    Address = "14 Perry Cross Road, Bandra West",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Country = "India",
                    PostalCode = "400050",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    Phone = "+91 22 6100 2345",
                    Website = "https://glowlounge.bookline.local",
                    AverageRating = 4.8,
                    ReviewCount = 98,
                    VerificationStatus = VerificationStatus.Verified,
                    Currency = "USD",
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
                    Category = "Healthcare & Clinics",
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
                    Category = "Fitness & Training",
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
                    Category = "Photography & Media",
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
                    Category = "Professional Services",
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

            await db.SaveChangesAsync();

            // 3. Provider User with Multi-Business Ownership (Point 30)
            var providerUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == providerEmail);
            if (providerUser == null)
            {
                providerUser = new AppUser
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000099"),
                    TenantId = auraId, // Primary active business
                    Email = providerEmail,
                    PasswordHash = passwordHasher.HashPassword("Provider123!"),
                    FirstName = "Aarav",
                    LastName = "Singhania",
                    Phone = "+91 98250 11223",
                    Role = "Owner",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Users.Add(providerUser);
                await db.SaveChangesAsync();

                // Memberships in both Aura and Glow
                db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    TenantId = auraId,
                    UserId = providerUser.Id,
                    Role = "Owner",
                    JoinedAtUtc = DateTime.UtcNow
                });
                db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    TenantId = glowId,
                    UserId = providerUser.Id,
                    Role = "Owner",
                    JoinedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // 4. Seed Locations for Aura Wellness (Multi-Location: Point 31)
            var auraLoc1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
            var auraLoc2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
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
                await db.SaveChangesAsync();
            }

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

            // 7. Seed Products for Aura Wellness (Point 26, 27)
            if (!await db.Products.IgnoreQueryFilters().AnyAsync(p => p.TenantId == auraId))
            {
                db.Products.AddRange(
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        TenantId = auraId,
                        Name = "Botanical Rosewater Facial Mist",
                        Description = "Hydrosol distilled Bulgarian rose essence with hyaluronic acid to hydrate throughout the day.",
                        Price = 28.00m,
                        Currency = "USD",
                        Sku = "AURA-MIST-01",
                        StockQuantity = 35,
                        ReservedQuantity = 2,
                        SoldQuantity = 14,
                        IsActive = true,
                        IsPurchasableOnline = true
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        TenantId = auraId,
                        Name = "Organic French Lavender Essential Oil",
                        Description = "Single-origin pure therapeutic lavender oil for evening relaxation and restorative sleep.",
                        Price = 34.00m,
                        Currency = "USD",
                        Sku = "AURA-OIL-02",
                        StockQuantity = 25,
                        ReservedQuantity = 1,
                        SoldQuantity = 22,
                        IsActive = true,
                        IsPurchasableOnline = true
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        TenantId = auraId,
                        Name = "Herbal Nourishing Body Elixir",
                        Description = "Cold-pressed jojoba, sweet almond, and sandalwood oil blend for rich deep skin hydration.",
                        Price = 42.00m,
                        Currency = "USD",
                        Sku = "AURA-ELX-03",
                        StockQuantity = 18,
                        ReservedQuantity = 0,
                        SoldQuantity = 30,
                        IsActive = true,
                        IsPurchasableOnline = true
                    }
                );
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

                    // 1 upcoming booking
                    var upcoming = new Booking
                    {
                        Id = Guid.NewGuid(),
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
                    var past = new Booking
                    {
                        Id = Guid.NewGuid(),
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

                    // Favorite provider
                    db.Favorites.Add(new Favorite
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerUser.Id,
                        TenantId = auraId,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    // Customer review
                    db.Reviews.Add(new Review
                    {
                        Id = Guid.NewGuid(),
                        TenantId = auraId,
                        CustomerId = customerUser.Id,
                        BookingId = past.Id,
                        CustomerName = "Jane Customer",
                        Rating = 5,
                        Title = "Exceptional sanctuary & therapist",
                        Comment = "Dr. Priya was attentive and the bespoke oil blend smelled divine. The ambient lighting and calm atmosphere is unmatched in Ahmedabad.",
                        ProviderResponse = "Thank you so much Jane! It was our pleasure to host you at our Bodakdev flagship.",
                        ModerationStatus = ModerationStatus.Approved,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-4),
                        RespondedAtUtc = DateTime.UtcNow.AddDays(-3)
                    });

                    // Completed retail order
                    var order = new Order
                    {
                        Id = Guid.NewGuid(),
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
                    db.Orders.Add(order);

                    await db.SaveChangesAsync();
                }
            }

            logger.LogInformation("MarketplaceDbSeeder successfully populated multi-vendor marketplace data.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during MarketplaceDbSeeder execution.");
        }
    }
}
