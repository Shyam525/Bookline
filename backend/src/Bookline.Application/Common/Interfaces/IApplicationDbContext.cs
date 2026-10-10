using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<AppUser> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OrganizationMembership> OrganizationMemberships { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<Location> Locations { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<Service> Services { get; }
    DbSet<DomainStaff> Staff { get; }
    DbSet<StaffService> StaffServices { get; }
    DbSet<WorkingHours> WorkingHours { get; }
    DbSet<TimeOff> TimeOffs { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Product> Products { get; }
    DbSet<Resource> Resources { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Favorite> Favorites { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Commission> Commissions { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<BookingHold> BookingHolds { get; }
    DbSet<WebhookSubscription> Webhooks { get; }
    DbSet<WebhookDeliveryLog> WebhookLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<NotificationSetting> NotificationSettings { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
