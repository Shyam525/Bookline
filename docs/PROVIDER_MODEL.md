# Provider Model Architecture

## 1. Provider Entity Overview
A Provider in Bookline is represented by the `Tenant` entity (`Bookline.Domain.Entities.Tenant`), serving as the administrative and financial boundary for businesses on the platform.

```csharp
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string City { get; set; } = "Ahmedabad";
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; } = 23.0225;
    public double Longitude { get; set; } = 72.5714;
    public double AverageRating { get; set; } = 5.0;
    public int ReviewCount { get; set; } = 0;
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public decimal PlatformCommissionPercent { get; set; } = 10.0m;
    public decimal PendingPayoutBalance { get; set; } = 0.0m;
    public decimal AvailablePayoutBalance { get; set; } = 0.0m;
    public decimal PaidOutBalance { get; set; } = 0.0m;
    public bool IsPublished { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
```

## 2. Multi-Storefront & Multi-Location Hierarchy
Providers can manage multiple physical locations, teams, services, and inventory:
- **Locations** (`Bookline.Domain.Entities.Location`): Physical branches with coordinates, working hours, and phone numbers.
- **Staff** (`Bookline.Domain.Entities.Staff`): Specialists, practitioners, and technicians assigned to locations.
- **Services** (`Bookline.Domain.Entities.Service`): Service catalog with duration, pricing, buffer times, and categorization.
- **Products** (`Bookline.Domain.Entities.Product`): Physical inventory sold online or in-store with stock tracking.

## 3. Financial & Payout Lifecycle
1. **Gross Revenue**: Collected via Stripe or Razorpay.
2. **Platform Commission Deduction**: Fixed or tier-based percentage (e.g., 10%) retained automatically.
3. **Provider Net Balance**: Credited to `AvailablePayoutBalance`.
4. **Disbursement**: Payout requested to designated bank account or UPI ID via `IPayoutProvider`.
