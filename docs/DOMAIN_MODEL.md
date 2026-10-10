# Bookline Domain Model Architecture

## 1. Domain Entities & Aggregates

Bookline follows strict Domain-Driven Design (DDD). The domain layer encapsulates all entities, invariants, value objects, and state machine transitions with zero external framework dependencies.

```text
                               ┌─────────────────┐
                               │     Tenant      │
                               └────────┬────────┘
                                        │ 1..*
      ┌──────────────┬──────────────────┼──────────────────┬─────────────────┐
      │              │                  │                  │                 │
┌─────▼──────┐ ┌─────▼──────┐     ┌─────▼──────┐     ┌─────▼──────┐    ┌─────▼──────┐
│  Location  │ │   Staff    │     │  Service   │     │  Product   │    │  Customer  │
└─────┬──────┘ └─────┬──────┘     └─────┬──────┘     └─────┬──────┘    └─────┬──────┘
      │              │                  │                  │                 │
      └──────────────┼──────────────────┴──────────────────┼─────────────────┘
                     │                                     │
               ┌─────▼──────┐                        ┌─────▼──────┐
               │  Booking   │                        │   Order    │
               └─────┬──────┘                        └─────┬──────┘
                     │                                     │
                     └──────────────────┬──────────────────┘
                                        │
                                  ┌─────▼──────┐
                                  │  Payment   │
                                  └────────────┘
```

### Core Domain Entities

1. **Tenant** (`Bookline.Domain.Entities.Tenant`)
   - Root multi-tenant boundary.
   - Fields: `Id`, `Name`, `Slug`, `Category`, `BusinessType`, `City`, `Address`, `Latitude`, `Longitude`, `AverageRating`, `ReviewCount`, `VerificationStatus`, `PlatformCommissionPercent`, `PendingPayoutBalance`, `AvailablePayoutBalance`, `PaidOutBalance`, `IsPublished`, `IsActive`.

2. **Location** (`Bookline.Domain.Entities.Location`)
   - Physical venue branch.
   - Fields: `Id`, `TenantId`, `Name`, `Address`, `City`, `PostalCode`, `Latitude`, `Longitude`, `Phone`, `Email`, `Timezone`, `IsActive`.

3. **Staff** (`Bookline.Domain.Entities.Staff`)
   - Service providers, practitioners, specialists.
   - Fields: `Id`, `TenantId`, `LocationId`, `Name`, `Email`, `Phone`, `Role`, `Bio`, `AvatarUrl`, `IsActive`.

4. **Service** (`Bookline.Domain.Entities.Service`)
   - Bookable treatments and appointments.
   - Fields: `Id`, `TenantId`, `CategoryId`, `Name`, `Description`, `DurationMinutes`, `BufferBeforeMinutes`, `BufferAfterMinutes`, `Price`, `Currency`, `IsActive`.

5. **Product** (`Bookline.Domain.Entities.Product`)
   - Physical retail merchandise.
   - Invariants: `StockQuantity >= 0` (Section 133). `AvailableQuantity => Math.Max(0, StockQuantity - ReservedQuantity)`.
   - Fields: `Id`, `TenantId`, `Name`, `Description`, `Price`, `Currency`, `StockQuantity`, `ReservedQuantity`, `SoldQuantity`, `IsActive`.

6. **Customer** (`Bookline.Domain.Entities.Customer`)
   - Client records and CRM profiles.
   - Fields: `Id`, `TenantId`, `FirstName`, `LastName`, `Email`, `Phone`, `TotalSpentAmount`, `AppointmentCount`, `LastVisitUtc`.

7. **Booking** (`Bookline.Domain.Entities.Booking`)
   - Authoritative appointment record.
   - State Machine: `Pending` -> `Confirmed` -> `Completed` / `Cancelled` / `NoShow` / `Rescheduled`.
   - Invariants: Zero double booking. Overlapping staff time intervals are strictly rejected.

8. **Order & OrderItem** (`Bookline.Domain.Entities.Order`, `OrderItem`)
   - Physical commerce transactions.
   - State Machine: `Pending` -> `Paid` -> `Processing` -> `Ready` -> `Completed` / `Cancelled` / `Refunded`.

9. **Payment & Payout** (`Bookline.Domain.Entities.Payment`, `Payout`)
   - Financial transactions and ledger entries.
   - States: `Pending`, `Completed`, `Failed`, `Refunded`.

10. **Review** (`Bookline.Domain.Entities.Review`)
    - Customer rating (1–5) and verified visit feedback with moderation status.

11. **IdempotencyRecord** (`Bookline.Domain.Entities.IdempotencyRecord`)
    - Guarantees repeatable operation safety across bookings, orders, payments, refunds, and webhooks (Section 121 & 132).

12. **OutboxMessage** (`Bookline.Domain.Entities.OutboxMessage`)
    - Guaranteed at-least-once asynchronous event delivery with transactional outbox pattern.
