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
    public DbSet<Service> Services => Set<Service>();
    public DbSet<DomainStaff> Staff => Set<DomainStaff>();
    public DbSet<StaffService> StaffServices => Set<StaffService>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WebhookSubscription> Webhooks => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookLogs => Set<WebhookDeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for StaffService
        modelBuilder.Entity<StaffService>()
            .HasKey(ss => new { ss.StaffId, ss.ServiceId });

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Status).HasConversion<string>();
            builder.Property(b => b.RowVersion).IsRowVersion();
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
}
