using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Customer : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AvatarUrl { get; set; }
    public int TotalBookingsCount { get; set; } = 0;
    public decimal TotalSpentAmount { get; set; } = 0.00m;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName => string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}".Trim();
}
