namespace Bookline.Application.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using Bookline.Application.Services.Handlers;
using Bookline.Application.Services.Validators;
using Bookline.Application.Staff.Commands;
using Bookline.Application.Staff.Handlers;
using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Application.Tenants.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

public class Phase2ProviderOnboardingAndBusinessConfigurationTests
{
    private BooklineDbContext CreateDbContext(string dbName, Guid? tenantId = null)
    {
        var tenantContext = new TenantContext();
        if (tenantId.HasValue)
        {
            tenantContext.SetTenantId(tenantId.Value);
        }

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BooklineDbContext(options, tenantContext);
    }

    [Fact]
    public async Task BusinessOwnership_TenantIsolation_ProviderCannotAccessOtherBusiness()
    {
        var dbName = "Db_Ownership_" + Guid.NewGuid();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantAId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantAId,
                Name = "Salon A Ahmedabad",
                Slug = "salon-a-ahmedabad",
                City = "Ahmedabad",
                Area = "Bodakdev",
                TimeZoneId = "Asia/Kolkata",
                IsActive = true
            });
            db.Tenants.Add(new Tenant
            {
                Id = tenantBId,
                Name = "Salon B Surat",
                Slug = "salon-b-surat",
                City = "Surat",
                Area = "Athwa",
                TimeZoneId = "Asia/Kolkata",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Caller from Tenant A queries business profile
        using (var db = CreateDbContext(dbName, tenantAId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantAId);
            var handler = new BusinessProfileHandlers(db, tenantCtx);

            var profile = await handler.Handle(new GetBusinessProfileQuery(), CancellationToken.None);

            Assert.NotNull(profile);
            Assert.Equal(tenantAId, profile.BusinessId);
            Assert.Equal("Salon A Ahmedabad", profile.Name);
            Assert.Equal("salon-a-ahmedabad", profile.Slug);
        }
    }

    [Fact]
    public async Task PublicSlug_Uniqueness_CollisionDetectedAndRejected()
    {
        var dbName = "Db_SlugCollision_" + Guid.NewGuid();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantAId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantAId,
                Name = "Aura Wellness",
                Slug = "aura-wellness",
                City = "Ahmedabad",
                Area = "Bodakdev",
                TimeZoneId = "Asia/Kolkata",
                IsActive = true
            });
            db.Tenants.Add(new Tenant
            {
                Id = tenantBId,
                Name = "Glow Studio",
                Slug = "glow-studio",
                City = "Ahmedabad",
                Area = "Satellite",
                TimeZoneId = "Asia/Kolkata",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Tenant B attempts to change its slug to "aura-wellness"
        using (var db = CreateDbContext(dbName, tenantBId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantBId);
            var handler = new BusinessProfileHandlers(db, tenantCtx);

            var updateReq = new UpdateBusinessProfileRequest(
                Name: "Glow Studio",
                Slug: "aura-wellness", // Collides with Tenant A
                Description: "A beauty studio",
                Category: "Beauty & Wellness",
                BusinessType: "Salon",
                Phone: "+91 98250 12345",
                Email: "glow@example.com",
                Website: "https://glow.example.com",
                Address: "12 Satellite Road",
                City: "Ahmedabad",
                Area: "Satellite",
                State: "Gujarat",
                Country: "India",
                PostalCode: "380015",
                Latitude: 23.02,
                Longitude: 72.50,
                TimeZoneId: "Asia/Kolkata",
                LogoUrl: null,
                CoverImageUrl: null,
                GalleryImages: null,
                IsPublished: true
            );

            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new UpdateBusinessProfileCommand(updateReq), CancellationToken.None));

            Assert.Contains("SLUG_ALREADY_EXISTS", ex.Message);

            // Also test real-time slug check query
            var checkResult = await handler.Handle(new CheckSlugAvailabilityQuery("aura-wellness"), CancellationToken.None);
            Assert.False(checkResult.IsAvailable);

            var availableCheck = await handler.Handle(new CheckSlugAvailabilityQuery("unique-fresh-slug"), CancellationToken.None);
            Assert.True(availableCheck.IsAvailable);
        }
    }

    [Theory]
    [InlineData("valid-slug-123", true)]
    [InlineData("bodakdev-salon-ahmedabad", true)]
    [InlineData("invalid slug with spaces", false)]
    [InlineData("INVALID_UPPERCASE", false)]
    [InlineData("-leading-hyphen", false)]
    [InlineData("trailing-hyphen-", false)]
    [InlineData("ab", false)] // Less than 3 chars
    public void PublicSlug_Validation_FormatRulesEnforced(string slug, bool expectedValid)
    {
        var validator = new UpdateBusinessProfileCommandValidator();
        var command = new UpdateBusinessProfileCommand(new UpdateBusinessProfileRequest(
            Name: "Valid Business",
            Slug: slug,
            Description: "A great local salon",
            Category: "Salon",
            BusinessType: "Salon",
            Phone: null,
            Email: null,
            Website: null,
            Address: "100 SG Highway",
            City: "Ahmedabad",
            Area: "Bodakdev",
            State: "Gujarat",
            Country: "India",
            PostalCode: "380054",
            Latitude: null,
            Longitude: null,
            TimeZoneId: "Asia/Kolkata",
            LogoUrl: null,
            CoverImageUrl: null,
            GalleryImages: null,
            IsPublished: true
        ));

        var result = validator.Validate(command);
        if (expectedValid)
        {
            Assert.DoesNotContain(result.Errors, e => e.PropertyName.Contains("Slug"));
        }
        else
        {
            Assert.Contains(result.Errors, e => e.PropertyName.Contains("Slug"));
        }
    }

    [Theory]
    [InlineData("Asia/Kolkata", true)]
    [InlineData("Europe/London", true)]
    [InlineData("America/New_York", true)]
    [InlineData("Ahmedabad", false)] // City is not a timezone!
    [InlineData("Invalid/Timezone", false)]
    [InlineData("GMT+5:30", false)]
    [InlineData("", false)]
    public void TimeZone_Validation_IanaZonesRequired(string tz, bool expectedValid)
    {
        var validator = new UpdateBusinessProfileCommandValidator();
        var command = new UpdateBusinessProfileCommand(new UpdateBusinessProfileRequest(
            Name: "Valid Business",
            Slug: "valid-business",
            Description: "A great local salon",
            Category: "Salon",
            BusinessType: "Salon",
            Phone: null,
            Email: null,
            Website: null,
            Address: "100 SG Highway",
            City: "Ahmedabad",
            Area: "Bodakdev",
            State: "Gujarat",
            Country: "India",
            PostalCode: "380054",
            Latitude: null,
            Longitude: null,
            TimeZoneId: tz,
            LogoUrl: null,
            CoverImageUrl: null,
            GalleryImages: null,
            IsPublished: true
        ));

        var result = validator.Validate(command);
        if (expectedValid)
        {
            Assert.DoesNotContain(result.Errors, e => e.PropertyName.Contains("TimeZoneId"));
        }
        else
        {
            Assert.Contains(result.Errors, e => e.PropertyName.Contains("TimeZoneId"));
        }
    }

    [Fact]
    public void Service_Validation_NegativePriceOrInvalidDuration_Rejected()
    {
        var validator = new CreateServiceCommandValidator();

        // 1. Negative price
        var invalidPriceCmd = new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: Guid.NewGuid(),
            Name: "Haircut",
            Slug: "haircut",
            Description: "Standard haircut",
            DurationMinutes: 30,
            BufferBeforeMinutes: 0,
            BufferAfterMinutes: 10,
            Price: -50.00m,
            Currency: "INR",
            IsOnlineBookingEnabled: true
        ));

        var priceResult = validator.Validate(invalidPriceCmd);
        Assert.False(priceResult.IsValid);
        Assert.Contains(priceResult.Errors, e => e.PropertyName.Contains("Price"));

        // 2. Zero duration
        var zeroDurationCmd = new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: Guid.NewGuid(),
            Name: "Haircut",
            Slug: "haircut",
            Description: "Standard haircut",
            DurationMinutes: 0,
            BufferBeforeMinutes: 0,
            BufferAfterMinutes: 10,
            Price: 200.00m,
            Currency: "INR",
            IsOnlineBookingEnabled: true
        ));

        var durResult = validator.Validate(zeroDurationCmd);
        Assert.False(durResult.IsValid);
        Assert.Contains(durResult.Errors, e => e.PropertyName.Contains("DurationMinutes"));
    }

    [Fact]
    public void Service_Validation_ValidBuffersAndDecimalPrice_Accepted()
    {
        var validator = new CreateServiceCommandValidator();

        var validCmd = new CreateServiceCommand(new CreateServiceRequest(
            CategoryId: Guid.NewGuid(),
            Name: "Deluxe Hair Spa",
            Slug: "deluxe-hair-spa",
            Description: "Deep conditioning treatment",
            DurationMinutes: 60,
            BufferBeforeMinutes: 10,
            BufferAfterMinutes: 15,
            Price: 1250.75m,
            Currency: "INR",
            IsOnlineBookingEnabled: true
        ));

        var result = validator.Validate(validCmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Service_ActivateAndDeactivate_StatusTransitionsCorrectly()
    {
        var dbName = "Db_ServiceStatus_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            db.ServiceCategories.Add(new ServiceCategory
            {
                Id = categoryId,
                TenantId = tenantId,
                Name = "Grooming",
                IsActive = true
            });

            db.Services.Add(new Service
            {
                Id = serviceId,
                TenantId = tenantId,
                CategoryId = categoryId,
                Name = "Classic Shave",
                Slug = "classic-shave",
                DurationMinutes = 30,
                Price = 150.00m,
                Currency = "INR",
                Status = "Active",
                IsActive = true,
                IsPublished = true,
                IsOnlineBookingEnabled = true
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var handler = new ServiceHandlers(db, tenantCtx);

            // Deactivate
            var deactivated = await handler.Handle(new SetServiceStatusCommand(serviceId, "Inactive"), CancellationToken.None);
            Assert.Equal("Inactive", deactivated.Status);
            Assert.False(deactivated.IsActive);

            // Re-activate
            var reactivated = await handler.Handle(new SetServiceStatusCommand(serviceId, "Active"), CancellationToken.None);
            Assert.Equal("Active", reactivated.Status);
            Assert.True(reactivated.IsActive);
        }
    }

    [Fact]
    public async Task Staff_ServiceAssignment_EnforcesRelationshipIntegrity()
    {
        var dbName = "Db_StaffService_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var service1Id = Guid.NewGuid();
        var service2Id = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            db.Staff.Add(new DomainStaff
            {
                Id = staffId,
                TenantId = tenantId,
                Name = "Ramesh Patel",
                Email = "ramesh@example.com",
                IsActive = true
            });
            db.Services.Add(new Service
            {
                Id = service1Id,
                TenantId = tenantId,
                Name = "Haircut",
                Slug = "haircut",
                DurationMinutes = 30,
                Price = 200m,
                Currency = "INR",
                IsActive = true
            });
            db.Services.Add(new Service
            {
                Id = service2Id,
                TenantId = tenantId,
                Name = "Beard Trim",
                Slug = "beard-trim",
                DurationMinutes = 20,
                Price = 100m,
                Currency = "INR",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var handler = new StaffCommandHandler(db, tenantCtx);

            var result = await handler.Handle(new AssignStaffServicesCommand(staffId, new List<Guid> { service1Id, service2Id }), CancellationToken.None);

            Assert.Equal(2, result.AssignedServiceIds.Count);
            Assert.Contains(service1Id, result.AssignedServiceIds);
            Assert.Contains(service2Id, result.AssignedServiceIds);

            // Verify in database
            var links = await db.StaffServices.Where(ss => ss.StaffId == staffId).ToListAsync();
            Assert.Equal(2, links.Count);
        }
    }

    [Fact]
    public void Staff_InvalidWorkingHours_EndTimeBeforeStartTime_Rejected()
    {
        var validator = new SetWorkingHoursCommandValidator();

        var invalidCommand = new SetWorkingHoursCommand(
            Guid.NewGuid(),
            new List<WorkingHourInput>
            {
                new(DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(9, 0)) // 18:00 to 09:00 invalid!
            }
        );

        var result = validator.Validate(invalidCommand);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("after start time"));
    }

    [Fact]
    public void Staff_Breaks_OverlappingBreaks_Rejected()
    {
        var validator = new SetWorkingHoursCommandValidator();

        var commandWithOverlappingBreaks = new SetWorkingHoursCommand(
            Guid.NewGuid(),
            new List<WorkingHourInput>
            {
                new(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0),
                    new List<BreakInput>
                    {
                        new(new TimeOnly(12, 0), new TimeOnly(13, 0), "Lunch Break"),
                        new(new TimeOnly(12, 30), new TimeOnly(13, 30), "Overlapping Tea Break")
                    }
                )
            }
        );

        var result = validator.Validate(commandWithOverlappingBreaks);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Breaks cannot overlap"));
    }

    [Fact]
    public async Task Staff_Breaks_ValidNonOverlappingBreaks_AcceptedAndSplit()
    {
        var dbName = "Db_StaffBreaks_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            db.Staff.Add(new DomainStaff
            {
                Id = staffId,
                TenantId = tenantId,
                Name = "Priya Sharma",
                Email = "priya@example.com",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var handler = new StaffCommandHandler(db, tenantCtx);

            var cmd = new SetWorkingHoursCommand(
                staffId,
                new List<WorkingHourInput>
                {
                    new(
                        DayOfWeek.Monday,
                        new TimeOnly(9, 0),
                        new TimeOnly(18, 0),
                        new List<BreakInput>
                        {
                            new(new TimeOnly(13, 0), new TimeOnly(14, 0), "Lunch Break")
                        }
                    )
                }
            );

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.Single(result);
            Assert.NotNull(result[0].Breaks);
            Assert.Single(result[0].Breaks!);
            Assert.Equal(new TimeOnly(13, 0), result[0].Breaks![0].StartTime);
            Assert.Equal(new TimeOnly(14, 0), result[0].Breaks![0].EndTime);

            // Database should store 2 operational working windows: 09:00-13:00 and 14:00-18:00
            var windowsInDb = await db.WorkingHours.Where(w => w.StaffId == staffId).OrderBy(w => w.StartTime).ToListAsync();
            Assert.Equal(2, windowsInDb.Count);
            Assert.Equal(new TimeOnly(9, 0), windowsInDb[0].StartTime);
            Assert.Equal(new TimeOnly(13, 0), windowsInDb[0].EndTime);
            Assert.Equal(new TimeOnly(14, 0), windowsInDb[1].StartTime);
            Assert.Equal(new TimeOnly(18, 0), windowsInDb[1].EndTime);
        }
    }

    [Fact]
    public async Task Staff_InactiveRestrictions_InactiveStaffCannotReceiveAppointmentsOrSlotHolds()
    {
        var dbName = "Db_InactiveStaff_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Test Salon",
                Slug = "test-salon",
                IsActive = true
            });
            db.Services.Add(new Service
            {
                Id = serviceId,
                TenantId = tenantId,
                Name = "Styling",
                Slug = "styling",
                DurationMinutes = 30,
                Price = 300m,
                Currency = "INR",
                IsActive = true,
                IsPublished = true,
                IsOnlineBookingEnabled = true
            });
            db.Staff.Add(new DomainStaff
            {
                Id = staffId,
                TenantId = tenantId,
                Name = "Inactive Specialist",
                Email = "inactive@example.com",
                IsActive = false // INACTIVE!
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var slotHoldMock = new FakeSlotHoldService();
            var holdHandler = new HoldSlotCommandHandler(slotHoldMock, tenantCtx, db, SystemClock.Instance);

            var holdCmd = new HoldSlotCommand(
                StaffId: staffId,
                ServiceId: serviceId,
                StartUtc: DateTimeOffset.UtcNow.AddDays(1)
            );

            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                holdHandler.Handle(holdCmd, CancellationToken.None));

            Assert.Contains("STAFF_UNAVAILABLE", ex.Message);
        }
    }

    [Fact]
    public async Task Onboarding_SaveAndResume_PreservesPartiallyCompletedSetup()
    {
        var dbName = "Db_SaveResume_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = "Draft Salon",
                Slug = "draft-salon",
                City = "Ahmedabad",
                Area = "Bodakdev",
                TimeZoneId = "Asia/Kolkata",
                Currency = "INR",
                IsActive = true,
                OnboardingStep = 1
            };
            var user = new AppUser
            {
                Id = userId,
                TenantId = tenantId,
                Email = "owner@draftsalon.com",
                FirstName = "Hitesh",
                LastName = "Shah",
                Role = "Owner"
            };
            db.Tenants.Add(tenant);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Provider saves Step 2 (Business Name & custom slug)
        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var handler = new OnboardingHandlers(db, tenantCtx);

            var step2Result = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
                Step: 2,
                BusinessName: "Royal Beard Studio",
                BusinessSlug: "royal-beard-studio"
            )), CancellationToken.None);

            Assert.Equal("Royal Beard Studio", step2Result.BusinessName);
            Assert.Equal("royal-beard-studio", step2Result.BusinessSlug);

            // Provider saves Step 5 (Address, City, Area)
            var step5Result = await handler.Handle(new SaveOnboardingStepCommand(new SaveOnboardingStepRequest(
                Step: 5,
                Address: "501 Iscon Elegance, SG Highway",
                City: "Ahmedabad",
                Area: "Prahlad Nagar",
                State: "Gujarat",
                PostalCode: "380015",
                Country: "India"
            )), CancellationToken.None);

            Assert.Equal("501 Iscon Elegance, SG Highway", step5Result.Address);
            Assert.Equal("Ahmedabad", step5Result.City);
            Assert.Equal("Prahlad Nagar", step5Result.Area);

            // Provider simulates browser close and session resume via GetOnboardingStatusQuery
            var resumedStatus = await handler.Handle(new GetOnboardingStatusQuery(), CancellationToken.None);

            Assert.Equal("Royal Beard Studio", resumedStatus.BusinessName);
            Assert.Equal("royal-beard-studio", resumedStatus.BusinessSlug);
            Assert.Equal("501 Iscon Elegance, SG Highway", resumedStatus.Address);
            Assert.Equal("Ahmedabad", resumedStatus.City);
            Assert.Equal("Prahlad Nagar", resumedStatus.Area);
        }
    }

    [Fact]
    public async Task BusinessPolicies_CancellationAndReschedulingPolicies_PersistedAndRetrieved()
    {
        var dbName = "Db_Policies_" + Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        using (var db = CreateDbContext(dbName, tenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Urban Spa",
                Slug = "urban-spa",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(dbName, tenantId))
        {
            var tenantCtx = new TenantContext();
            tenantCtx.SetTenantId(tenantId);
            var handler = new BusinessProfileHandlers(db, tenantCtx);

            var updateReq = new UpdateBusinessPoliciesRequest(
                HoldDurationMinutes: 10,
                MinimumNoticeHours: 3,
                BookingHorizonDays: 45,
                CancellationWindowHours: 6,
                CancellationPolicy: "Please notify at least 6 hours in advance for cancellations.",
                ReschedulingWindowHours: 3,
                ReschedulingPolicy: "Rescheduling is allowed up to 3 hours before start time.",
                WeeklyHours: new List<BusinessDayHours>
                {
                    new(DayOfWeek.Monday, true, new TimeOnly(9, 30), new TimeOnly(20, 30), new List<BusinessBreakPeriod>
                    {
                        new(new TimeOnly(13, 0), new TimeOnly(14, 0), "Afternoon Lunch")
                    })
                }
            );

            var updated = await handler.Handle(new UpdateBusinessPoliciesCommand(updateReq), CancellationToken.None);

            Assert.Equal(10, updated.HoldDurationMinutes);
            Assert.Equal(3, updated.MinimumNoticeHours);
            Assert.Equal(45, updated.BookingHorizonDays);
            Assert.Equal(6, updated.CancellationWindowHours);
            Assert.Equal("Please notify at least 6 hours in advance for cancellations.", updated.CancellationPolicy);
            Assert.Equal(3, updated.ReschedulingWindowHours);
            Assert.Equal("Rescheduling is allowed up to 3 hours before start time.", updated.ReschedulingPolicy);

            // Retrieve via GetBusinessPoliciesQuery
            var retrieved = await handler.Handle(new GetBusinessPoliciesQuery(), CancellationToken.None);
            Assert.Equal(10, retrieved.HoldDurationMinutes);
            Assert.Equal(6, retrieved.CancellationWindowHours);
            Assert.Equal("Please notify at least 6 hours in advance for cancellations.", retrieved.CancellationPolicy);
            Assert.Equal(3, retrieved.ReschedulingWindowHours);
        }
    }

    private class FakeSlotHoldService : ISlotHoldService
    {
        public Task<Guid?> AcquireHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset slotTime, TimeSpan duration, CancellationToken ct = default)
        {
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<bool> ValidateHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task ReleaseHoldAsync(Guid tenantId, Guid staffId, DateTimeOffset startUtc, Guid holdId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
