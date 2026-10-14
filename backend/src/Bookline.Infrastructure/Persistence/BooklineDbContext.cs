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
    public DbSet<ServiceLocation> ServiceLocations => Set<ServiceLocation>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
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
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for StaffService and ServiceLocation
        modelBuilder.Entity<StaffService>()
            .HasKey(ss => new { ss.StaffId, ss.ServiceId });

        modelBuilder.Entity<ServiceLocation>()
            .HasKey(sl => new { sl.ServiceId, sl.LocationId });

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => t.Slug).IsUnique();
            builder.HasIndex(t => new { t.City, t.Category, t.IsPublished });
            builder.HasIndex(t => new { t.Latitude, t.Longitude });
            builder.Property(t => t.VerificationStatus).HasConversion<string>();
            builder.Property(t => t.ModerationStatus).HasConversion<string>();
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
            builder.HasIndex(l => new { l.TenantId, l.Name, l.IsActive });
            builder.HasIndex(l => new { l.TenantId, l.City, l.IsActive });
            builder.HasIndex(l => new { l.Latitude, l.Longitude });
        });

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);
            builder.HasIndex(b => b.BookingReference).IsUnique();
            builder.HasIndex(b => new { b.TenantId, b.StaffId, b.StartUtc, b.EndUtc });
            builder.HasIndex(b => new { b.TenantId, b.LocationId, b.StartUtc });
            builder.HasIndex(b => new { b.TenantId, b.ServiceId });
            builder.HasIndex(b => new { b.TenantId, b.Status, b.StartUtc });
            builder.HasIndex(b => new { b.CustomerId, b.Status });
            builder.HasIndex(b => b.RelatedOrderId);
            builder.Property(b => b.Status).HasConversion<string>();
            builder.Property(b => b.TotalPrice).HasPrecision(18, 2);
            builder.Property(b => b.DepositPaid).HasPrecision(18, 2);
            builder.Property(b => b.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
        });

        modelBuilder.Entity<DomainStaff>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.TenantId, s.IsActive });
            builder.HasIndex(s => new { s.TenantId, s.Email });
        });

        modelBuilder.Entity<OrganizationMembership>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.HasIndex(m => new { m.UserId, m.TenantId }).IsUnique();
            builder.HasIndex(m => new { m.TenantId, m.Role });
            builder.HasIndex(m => m.UserId);
        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.HasIndex(c => new { c.TenantId, c.Email });
            builder.HasIndex(c => new { c.TenantId, c.Phone });
            builder.HasIndex(c => new { c.TenantId, c.UserId });
            builder.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<WorkingHours>(builder =>
        {
            builder.HasKey(w => w.Id);
            builder.HasIndex(w => new { w.TenantId, w.StaffId, w.DayOfWeek });
        });

        modelBuilder.Entity<TimeOff>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => new { t.TenantId, t.StaffId, t.StartUtc, t.EndUtc });
        });

        modelBuilder.Entity<ProductCategory>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.HasIndex(c => new { c.TenantId, c.Slug });
            builder.HasIndex(c => new { c.TenantId, c.IsActive });
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.TenantId, p.IsActive, p.IsPurchasableOnline });
            builder.HasIndex(p => new { p.TenantId, p.Slug });
            builder.HasIndex(p => new { p.TenantId, p.Sku });
            builder.Property(p => p.Price).HasPrecision(18, 2);
            builder.Property(p => p.ModerationStatus).HasConversion<string>();
            builder.Property(p => p.RowVersion).IsConcurrencyToken().ValueGeneratedNever();

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => new { i.TenantId, i.ProductId });
        });

        modelBuilder.Entity<Service>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.TenantId, s.IsActive, s.IsOnlineBookingEnabled });
            builder.HasIndex(s => new { s.TenantId, s.Slug });
            builder.Property(s => s.Price).HasPrecision(18, 2);
            builder.Property(s => s.ModerationStatus).HasConversion<string>();
            builder.Property(s => s.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            builder.HasOne(s => s.RequiredResource)
                .WithMany()
                .HasForeignKey(s => s.RequiredResourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Resource>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.HasIndex(r => new { r.TenantId, r.LocationId, r.IsActive });
            builder.HasOne(r => r.Location)
                .WithMany()
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasIndex(o => new { o.TenantId, o.Status });
            builder.HasIndex(o => o.CustomerId);
            builder.HasIndex(o => o.RelatedBookingId);
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
            builder.HasIndex(n => new { n.TenantId, n.CreatedAtUtc });
            builder.HasIndex(n => new { n.TenantId, n.RecipientEmail });
            builder.Property(n => n.NotificationType).HasConversion<string>();
            builder.Property(n => n.Channel).HasConversion<string>();
            builder.Property(n => n.Status).HasConversion<string>();
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => new { a.TenantId, a.CreatedAtUtc });
            builder.HasIndex(a => new { a.TenantId, a.Action });
        });

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentType).HasConversion<string>();
            builder.Property(p => p.Status).HasConversion<string>();
            builder.Property(p => p.PaymentMethod).HasConversion<string>();
        });

        modelBuilder.Entity<Refund>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Amount).HasPrecision(18, 2);
            builder.HasIndex(r => new { r.TenantId, r.PaymentId });
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => new { i.Key, i.Operation }).IsUnique();
            builder.HasIndex(i => i.ExpiresAtUtc);
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
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => _tenantContext.IsSystem || e.TenantId == _tenantContext.TenantId);
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
        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
