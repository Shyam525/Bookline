# Customer Model Architecture

## 1. Domain Entity & Profile Specifications
The Customer entity (`Bookline.Domain.Entities.Customer`) represents both marketplace consumers and provider-specific client records.

```csharp
public class Customer : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalSpentAmount { get; set; } = 0.00m;
    public int AppointmentCount { get; set; } = 0;
    public DateTime? LastVisitUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

## 2. Customer Lifecycle & State Machine
1. **Discovery & Guest Mode**:
   - Guests can discover storefronts, inspect menus, check slots, and initiate checkout without prior authentication.
2. **Account Linking**:
   - During checkout, authenticated users link their `AppUser` identity via `CustomerId`.
   - Guest checkouts automatically create or associate customer records by email/phone matching.
3. **Engagement & Repeat Visits**:
   - Every completed appointment increments `AppointmentCount` and updates `LastVisitUtc`.
   - Total expenditures are aggregated onto `TotalSpentAmount` for VIP classification.

## 3. Privacy, Isolation & Security
- **Multi-Tenant Scoping**: Customer profiles belonging to a specific provider are strictly scoped by `TenantId`.
- **Global Query Filter**: Providers cannot view, query, or export customer data belonging to competitors.
- **Customer Notification Center**:
  - Unread notification count badge in navigation.
  - Event feeds for booking confirmations, reminders (24h/2h), order status updates, and refund notifications.
  - Granular read/unread tracking with instant mark-all-as-read.
