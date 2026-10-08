using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Common;
using Bookline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DomainStaff = Bookline.Domain.Entities.Staff;

namespace Bookline.Infrastructure.Persistence;

public class BooklineDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public BooklineDbContext(DbContextOptions<BooklineDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<DomainStaff> Staff => Set<DomainStaff>();
    public DbSet<StaffService> StaffServices => Set<StaffService>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<BookingHold> BookingHolds => Set<BookingHold>();
    public DbSet<WebhookSubscription> Webhooks => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookLogs => Set<WebhookDeliveryLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for StaffService
        modelBuilder.Entity<StaffService>()
            .HasKey(ss => new { ss.StaffId, ss.ServiceId });

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => t.Slug).IsUnique();
            builder.HasIndex(t => new { t.City, t.Category, t.IsPublished });
            builder.Property(t => t.VerificationStatus).HasConversion<string>();
            builder.Property(t => t.DepositType).HasConversion<string>();
            builder.Property(t => t.DepositAmount).HasPrecision(18, 2);
            builder.Property(t => t.CommissionRatePercentage).HasPrecision(18, 2);
            builder.Property(t => t.PendingPayoutBalance).HasPrecision(18, 2);
            builder.Property(t => t.AvailablePayoutBalance).HasPrecision(18, 2);
            builder.Property(t => t.PaidOutBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Location>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.HasIndex(l => new { l.TenantId, l.City, l.IsActive });
            builder.HasIndex(l => new { l.Latitude, l.Longitude });
        });

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);
            builder.HasIndex(b => b.BookingReference).IsUnique();
            builder.HasIndex(b => new { b.TenantId, b.StaffId, b.StartUtc, b.EndUtc });
            builder.HasIndex(b => new { b.CustomerId, b.Status });
            builder.Property(b => b.Status).HasConversion<string>();
            builder.Property(b => b.TotalPrice).HasPrecision(18, 2);
            builder.Property(b => b.DepositPaid).HasPrecision(18, 2);
            builder.Property(b => b.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.TenantId, p.IsActive, p.IsPurchasableOnline });
            builder.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasIndex(o => new { o.TenantId, o.Status });
            builder.HasIndex(o => o.CustomerId);
            builder.Property(o => o.Status).HasConversion<string>();
            builder.Property(o => o.Subtotal).HasPrecision(18, 2);
            builder.Property(o => o.Tax).HasPrecision(18, 2);
            builder.Property(o => o.TotalAmount).HasPrecision(18, 2);

            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
            builder.Property(i => i.TotalPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Review>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.HasIndex(r => new { r.TenantId, r.ModerationStatus });
            builder.HasIndex(r => r.CustomerId);
            builder.Property(r => r.ModerationStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Favorite>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.HasIndex(f => new { f.CustomerId, f.TenantId }).IsUnique();
            builder.HasOne(f => f.Tenant)
                .WithMany()
                .HasForeignKey(f => f.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Commission>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.HasIndex(c => new { c.TenantId, c.Status });
            builder.Property(c => c.GrossAmount).HasPrecision(18, 2);
            builder.Property(c => c.CommissionRatePercentage).HasPrecision(18, 2);
            builder.Property(c => c.CommissionAmount).HasPrecision(18, 2);
            builder.Property(c => c.ProviderNetAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payout>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.TenantId, p.Status });
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.Status).HasConversion<string>();
        });

        modelBuilder.Entity<BookingHold>(builder =>
        {
            builder.HasKey(h => h.Id);
            builder.HasIndex(h => h.HoldToken).IsUnique();
            builder.HasIndex(h => new { h.TenantId, h.StaffId, h.StartUtc, h.EndUtc, h.ExpiresAtUtc });
        });

        modelBuilder.Entity<NotificationLog>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.NotificationType).HasConversion<string>();
            builder.Property(n => n.Channel).HasConversion<string>();
            builder.Property(n => n.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentType).HasConversion<string>();
            builder.Property(p => p.Status).HasConversion<string>();
            builder.Property(p => p.PaymentMethod).HasConversion<string>();
        });

        // Apply global tenant filter on all TenantEntity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(BooklineDbContext)
                    .GetMethod(nameof(SetTenantQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefreshBookingRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RefreshBookingRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RefreshBookingRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Booking>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
