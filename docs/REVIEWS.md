# Reviews & Reputation System

## 1. Overview
The Reviews subsystem (`Bookline.Domain.Entities.Review`) provides verified feedback, ratings, and customer sentiment across marketplace providers and services.

## 2. Review Model Specifications
```csharp
public class Review : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ServiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; } = 5; // 1 to 5 stars
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedStay { get; set; } = true;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Approved;
    public string? ProviderResponse { get; set; }
    public DateTime? ProviderRespondedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

## 3. Seeded vs. Real Reviews (Section 125)
- Deterministic seed data reviews are clearly prefixed with `[Demo Seeded]` to prevent confusion during development and audit evaluations.
- Production reviews require a completed appointment or confirmed order to earn the `IsVerifiedStay` badge.

## 4. Moderation & Reputation Calculation
- **Rating Aggregation**: Whenever a new review is approved, the provider's `AverageRating` and `ReviewCount` on the `Tenant` entity are updated transactionally.
- **Provider Responses**: Providers can respond directly to customer reviews from their dashboard.
- **Reporting & Moderation**: Customers or providers can flag inappropriate content for Admin moderation.
