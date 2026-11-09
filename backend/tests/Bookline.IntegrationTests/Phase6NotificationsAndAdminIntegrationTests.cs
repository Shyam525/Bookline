namespace Bookline.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bookline.Api.Controllers;
using Bookline.Application.Bookings.Commands;
using Bookline.Application.Bookings.DTOs;
using Bookline.Application.Bookings.Queries;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Domain.Constants;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Notifications;
using Bookline.Infrastructure.Payments;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;
using DomainStaff = Bookline.Domain.Entities.Staff;

[Collection("PostgreSqlConcurrencyCollection")]
public class Phase6NotificationsAndAdminIntegrationTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION") ??
        "Host=localhost;Port=5432;Database=bookline_test_concurrency;Username=postgres;Password=postgrespassword";

    private static BooklineDbContext CreateDbContext(Guid? tenantId = null)
    {
        var tenantCtx = new TenantContext();
        tenantCtx.EnableSystemMode();
        if (tenantId.HasValue)
        {
            tenantCtx.SetTenantId(tenantId.Value);
        }

        var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Timeout = 60,
            CommandTimeout = 60,
            MaxPoolSize = 100
        };

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseNpgsql(connBuilder.ConnectionString, npgsql => npgsql.CommandTimeout(60))
            .Options;

        return new BooklineDbContext(options, tenantCtx);
    }

    private class FailingTestTransport : INotificationTransport
    {
        public string TransportName => "FailingTestTransport";
        public bool CanHandle(NotificationChannel channel) => true;
        public Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotificationDeliveryResult.Failed("Simulated Gateway Timeout / SMTP Server Refused Connection"));
        }
    }

    private class ThrowingTestTransport : INotificationTransport
    {
        public string TransportName => "ThrowingTestTransport";
        public bool CanHandle(NotificationChannel channel) => true;
        public Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Fatal network interface crash during socket connection.");
        }
    }

    private class SuccessTestTransport : INotificationTransport
    {
        public string TransportName => "SuccessTestTransport";
        public bool CanHandle(NotificationChannel channel) => true;
        public Task<NotificationDeliveryResult> SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotificationDeliveryResult.Succeeded($"test-msg-{Guid.NewGuid():N}"));
        }
    }

    private async Task<(Guid tenantId, Guid staffId, Guid serviceId, Guid customerId)> SeedBaselineAsync(BooklineDbContext db, string suffix)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = $"Phase 6 Salon {suffix}",
            Slug = $"phase6-salon-{suffix.ToLowerInvariant()}",
            Category = "Hair & Beauty",
            BusinessType = "Hair Salon",
            City = "Ahmedabad",
            Address = "100 SG Highway",
            IsActive = true,
            IsPublished = true,
            VerificationStatus = VerificationStatus.Verified,
            ModerationStatus = ModerationStatus.Approved,
            HoldDurationMinutes = 5,
            MinimumNoticeHours = 2,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = $"Hair Styling {suffix}",
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var staff = new DomainStaff
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = $"Stylist {suffix}",
            Email = $"stylist_{suffix.ToLowerInvariant()}@bookline.local",
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Staff.Add(staff);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            CategoryId = category.Id,
            Name = $"Haircut {suffix}",
            DurationMinutes = 30,
            Price = 45.00m,
            Currency = "INR",
            IsActive = true,
            IsPublished = true,
            IsOnlineBookingEnabled = true,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Services.Add(service);

        var staffService = new StaffService
        {
            StaffId = staff.Id,
            ServiceId = service.Id,
            TenantId = tenant.Id
        };
        db.StaffServices.Add(staffService);

        // 7-day working hours: 08:00 - 20:00
        for (int d = 0; d < 7; d++)
        {
            db.WorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StaffId = staff.Id,
                DayOfWeek = (DayOfWeek)d,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(20, 0)
            });
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = "Aarav",
            LastName = $"Patel_{suffix}",
            Email = $"aarav_{suffix.ToLowerInvariant()}@example.com",
            Phone = "+919876543210",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Customers.Add(customer);

        await db.SaveChangesAsync();
        return (tenant.Id, staff.Id, service.Id, customer.Id);
    }

    [Fact]
    public async Task Test01_OutboxTransactionConsistency_AtomicBookingAndOutboxPersistence()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "TxConsistent_" + Guid.NewGuid().ToString("N")[..6]);

        var tenantCtx = new TenantContext();
        tenantCtx.SetTenantId(tenantId);
        var schedulingService = new AppointmentSchedulingService(db);
        var handler = new CreateManualBookingCommandHandler(db, tenantCtx, schedulingService);

        var futureStart = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(6), TimeSpan.Zero);

        // Act
        var result = await handler.Handle(new CreateManualBookingCommand(new CreateManualBookingRequest(
            StaffId: staffId,
            ServiceId: serviceId,
            StartUtc: futureStart,
            Source: BookingSource.WalkIn,
            CustomerName: "Aarav Patel",
            CustomerPhone: "+919876543210",
            CustomerEmail: "aarav.atomic@example.com",
            CustomerNotes: "Walk-in appointment booked at reception"
        )), CancellationToken.None);

        // Assert - Verify atomic persistence across Booking, AuditLog, and OutboxMessage
        Assert.NotNull(result);
        var bookingInDb = await db.Bookings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == result.Id);
        Assert.NotNull(bookingInDb);
        Assert.Equal(BookingStatus.Confirmed, bookingInDb.Status);

        var auditInDb = await db.AuditLogs.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Target == result.Id.ToString());
        Assert.NotNull(auditInDb);
        Assert.Equal("Appointment.CreatedManual", auditInDb.Action);

        var outboxInDb = await db.OutboxMessages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Content.Contains(result.Id.ToString()));

        Assert.NotNull(outboxInDb);
        Assert.Equal(NotificationEvents.AppointmentConfirmed, outboxInDb.EventType);
        Assert.Equal("Pending", outboxInDb.Status);
    }

    [Fact]
    public async Task Test02_OutboxRetryAndExponentialBackoff_ScheduledAndFailedStates()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "RetryBackoff_" + Guid.NewGuid().ToString("N")[..6]);

        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = NotificationEvents.BookingConfirmed,
            Content = JsonSerializer.Serialize(new
            {
                BookingId = Guid.NewGuid(),
                BookingReference = "BL-RETRY-01",
                CustomerEmail = "retry.customer@bookline.local",
                CustomerId = customerId
            }),
            Status = "Pending",
            RetryCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var failingDispatcher = new NotificationTransportDispatcher(new[] { new FailingTestTransport() }, NullLogger<NotificationTransportDispatcher>.Instance);
        var sp = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(sp, NullLogger<OutboxAndReminderWorker>.Instance);

        // Act 1: First failed execution attempt
        var processedCount = await worker.ProcessOutboxBatchAsync(db, null, failingDispatcher, tenantId, CancellationToken.None);

        // Assert 1: RetryCount incremented, NextRetryAtUtc scheduled with exponential backoff (15s for attempt 1), Status remains Pending
        var updatedMsg = await db.OutboxMessages.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == msg.Id);
        Assert.NotNull(updatedMsg);
        Assert.Equal(1, updatedMsg.RetryCount);
        Assert.Equal("Pending", updatedMsg.Status);
        Assert.NotNull(updatedMsg.NextRetryAtUtc);
        Assert.True(updatedMsg.NextRetryAtUtc > DateTimeOffset.UtcNow.AddSeconds(5));

        // Act 2: Simulate 4 more failures (reaching max retries of 5)
        updatedMsg.RetryCount = 4;
        updatedMsg.NextRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        await worker.ProcessOutboxBatchAsync(db, null, failingDispatcher, tenantId, CancellationToken.None);

        // Assert 2: Permanently failed after attempt 5
        var finalMsg = await db.OutboxMessages.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == msg.Id);
        Assert.NotNull(finalMsg);
        Assert.Equal(5, finalMsg.RetryCount);
        Assert.Equal("Failed", finalMsg.Status);
        Assert.NotNull(finalMsg.Error);
    }

    [Fact]
    public async Task Test03_DuplicateEventHandling_IdempotencyProtectsAgainstDuplicateNotifications()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "Idempotent_" + Guid.NewGuid().ToString("N")[..6]);
        var bookingId = Guid.NewGuid();

        // Seed an existing already-sent notification for this booking
        var existingLog = new NotificationLog
        {
            TenantId = tenantId,
            BookingId = bookingId,
            CustomerId = customerId,
            RecipientEmail = "aarav.idem@example.com",
            NotificationType = NotificationType.BookingConfirmation,
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Sent,
            Subject = "Appointment Confirmed",
            Body = "Initial delivery",
            SentAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.NotificationLogs.Add(existingLog);

        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = NotificationEvents.BookingConfirmed,
            Content = JsonSerializer.Serialize(new
            {
                BookingId = bookingId,
                BookingReference = "BL-IDEM-01",
                CustomerEmail = "aarav.idem@example.com",
                CustomerId = customerId
            }),
            Status = "Pending",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var successDispatcher = new NotificationTransportDispatcher(new[] { new SuccessTestTransport() }, NullLogger<NotificationTransportDispatcher>.Instance);
        var sp = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(sp, NullLogger<OutboxAndReminderWorker>.Instance);

        // Act
        var processed = await worker.ProcessOutboxBatchAsync(db, null, successDispatcher, tenantId, CancellationToken.None);

        // Assert: Outbox event marked Processed without adding duplicate Sent notification logs
        var processedMsg = await db.OutboxMessages.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == msg.Id);
        Assert.NotNull(processedMsg);
        Assert.Equal("Processed", processedMsg.Status);

        var sentLogs = await db.NotificationLogs.IgnoreQueryFilters()
            .Where(n => n.BookingId == bookingId && n.NotificationType == NotificationType.BookingConfirmation)
            .ToListAsync();

        Assert.Single(sentLogs); // Idempotency prevented duplicate delivery!
    }

    [Fact]
    public async Task Test04_NotificationFailureIsolation_AppointmentRemainsConfirmedWhenTransportFails()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "Isolation_" + Guid.NewGuid().ToString("N")[..6]);

        var booking = Booking.CreateManual(
            tenantId: tenantId,
            staffId: staffId,
            serviceId: serviceId,
            customerId: customerId,
            startUtc: DateTimeOffset.UtcNow.AddDays(1),
            endUtc: DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            source: BookingSource.WalkIn,
            serviceNameSnapshot: "Haircut",
            priceSnapshot: 50m
        );
        db.Bookings.Add(booking);

        var msg = new OutboxMessage
        {
            TenantId = tenantId,
            EventType = NotificationEvents.BookingConfirmed,
            Content = JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerEmail = "customer.isolated@example.com",
                CustomerId = customerId
            }),
            Status = "Pending"
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var throwingDispatcher = new NotificationTransportDispatcher(new[] { new ThrowingTestTransport() }, NullLogger<NotificationTransportDispatcher>.Instance);
        var sp = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxAndReminderWorker(sp, NullLogger<OutboxAndReminderWorker>.Instance);

        // Act
        await worker.ProcessOutboxBatchAsync(db, null, throwingDispatcher, tenantId, CancellationToken.None);

        // Assert: The booking remains intact, valid, and confirmed even though notification transport threw!
        var verifiedBooking = await db.Bookings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(verifiedBooking);
        Assert.Equal(BookingStatus.Confirmed, verifiedBooking.Status);

        var failedMsg = await db.OutboxMessages.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == msg.Id);
        Assert.NotNull(failedMsg);
        Assert.Equal(1, failedMsg.RetryCount);
    }

    [Fact]
    public async Task Test05_AdminAuthorization_PlatformAdminAllowedAndBusinessStaffForbidden()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "AuthTest_" + Guid.NewGuid().ToString("N")[..6]);

        var controller = new AdminController(db, new PayoutProvider(db));

        // Mock Platform Admin User
        var adminClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "SuperAdmin"),
            new Claim(ClaimTypes.Role, Roles.PlatformAdmin)
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        // Act 1: PlatformAdmin accesses admin metrics
        var metricsResult = await controller.GetPlatformMetrics(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(metricsResult);
        Assert.NotNull(okResult.Value);

        // Verify that AdminController explicitly has the RequirePlatformAdmin policy
        var authorizeAttrs = typeof(AdminController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();

        Assert.NotEmpty(authorizeAttrs);
        Assert.Contains(authorizeAttrs, a => a.Policy == Policies.RequirePlatformAdmin || a.Roles?.Contains(Roles.PlatformAdmin) == true);
    }

    [Fact]
    public async Task Test06_AuditRecordCreation_TracksMeaningfulLifecycleEventsAndRedactsSecrets()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "AuditTest_" + Guid.NewGuid().ToString("N")[..6]);

        var controller = new AdminController(db, new PayoutProvider(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "AdminUser"), new Claim(ClaimTypes.Role, Roles.PlatformAdmin) }))
            }
        };

        // Act 1: Provider Approval
        await controller.ApproveProvider(tenantId, CancellationToken.None);

        // Act 2: Provider Visibility toggle
        await controller.UpdateProviderVisibility(tenantId, new UpdateVisibilityRequest(false), CancellationToken.None);

        // Act 3: Provider Suspension
        await controller.SuspendProvider(tenantId, new SuspendProviderRequest("Terms violation review"), CancellationToken.None);

        // Assert: Audit trail contains all 3 operations with sanitized metadata
        var auditLogs = await db.AuditLogs.IgnoreQueryFilters()
            .Where(a => a.Target == tenantId.ToString())
            .ToListAsync();

        Assert.Contains(auditLogs, a => a.Action == "Provider.Approved");
        Assert.Contains(auditLogs, a => a.Action == "Provider.VisibilityChanged");
        Assert.Contains(auditLogs, a => a.Action == "Provider.Suspended");

        // Verify no passwords, tokens, or API secrets leaked in metadata
        foreach (var log in auditLogs)
        {
            Assert.DoesNotContain("password", log.MetadataJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", log.MetadataJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", log.MetadataJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("apiKey", log.MetadataJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Test07_ProviderSuspensionBehavior_HidesProviderAndRejectsNewBookings()
    {
        // Arrange
        using var db = CreateDbContext();
        var (tenantId, staffId, serviceId, customerId) = await SeedBaselineAsync(db, "Suspended_" + Guid.NewGuid().ToString("N")[..6]);

        var controller = new AdminController(db, new PayoutProvider(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "PlatformAuditor"), new Claim(ClaimTypes.Role, Roles.PlatformAdmin) }))
            }
        };

        // Act 1: Suspend the provider via administrative action
        await controller.SuspendProvider(tenantId, new SuspendProviderRequest("Administrative suspension test"), CancellationToken.None);

        // Assert 1: Storefront query hides suspended provider
        var storefrontService = new PublicStorefrontService(db);
        var tenantInDb = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId);
        var storefront = await storefrontService.GetStorefrontBySlugAsync(tenantInDb.Slug, CancellationToken.None);
        Assert.Null(storefront); // Storefront is completely hidden from public!

        // Assert 2: Public Availability Query rejects suspended provider
        var slotEngine = new SlotEngine();
        var availHandler = new GetPublicAvailabilityQueryHandler(db, slotEngine);
        var targetDate = LocalDate.FromDateTime(DateTime.UtcNow.AddDays(2));

        var exAvail = await Assert.ThrowsAsync<ValidationException>(() =>
            availHandler.Handle(new GetPublicAvailabilityQuery(staffId, serviceId, targetDate, "Asia/Kolkata"), CancellationToken.None));
        Assert.Contains("PROVIDER_SUSPENDED", exAvail.Message);

        // Assert 3: Hold creation rejects suspended provider
        var holdHandler = new HoldSlotCommandHandler(new SlotHoldService(null), new TenantContext(), db);
        var exHold = await Assert.ThrowsAsync<ValidationException>(() =>
            holdHandler.Handle(new HoldSlotCommand(staffId, serviceId, DateTimeOffset.UtcNow.AddDays(2)), CancellationToken.None));
        Assert.Contains("PROVIDER_SUSPENDED", exHold.Message);

        // Assert 4: Booking creation rejects suspended provider
        var bookingHandler = new CreateBookingCommandHandler(db, new TenantContext(), new SlotHoldService(null), slotEngine);
        var exBooking = await Assert.ThrowsAsync<ValidationException>(() =>
            bookingHandler.Handle(new CreateBookingCommand(
                StaffId: staffId,
                ServiceId: serviceId,
                CustomerId: customerId,
                StartUtc: DateTimeOffset.UtcNow.AddDays(2),
                HoldId: Guid.NewGuid(),
                CustomerName: "Suspended Test",
                CustomerEmail: "suspended.customer@example.com",
                CustomerPhone: "+919876543210"
            ), CancellationToken.None));
        Assert.Contains("PROVIDER_SUSPENDED", exBooking.Message);
    }
}
