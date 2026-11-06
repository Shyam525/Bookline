# Bookline Target Architecture Specification

## 1. Architectural Philosophy & System Overview

Bookline is built as a **Modular Monolith** adhering to Domain-Driven Design (DDD) and Clean Architecture principles.

```
┌────────────────────────────────────────────────────────────────────────┐
│                              Bookline.Api                              │
│         Controllers, Middleware, Rate Limiting, OpenAPI Contracts       │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
┌───────────────────────────────────▼────────────────────────────────────┐
│                          Bookline.Application                          │
│     Use Cases, Commands, Queries, DTOs, FluentValidation, SlotEngine   │
└──────────────────┬────────────────────────────────┬────────────────────┘
                   │                                │
┌──────────────────▼───────────────┐ ┌──────────────▼────────────────────┐
│         Bookline.Domain          │ │      Bookline.Infrastructure      │
│ Pure Entities, Invariants, Enums │ │ EF Core, PostgreSQL, PostGIS,     │
│ Zero External NuGet Dependencies │ │ Redis Slot Holds, MailKit, Outbox │
└──────────────────────────────────┘ └───────────────────────────────────┘
```

The system prioritizes operational reliability, data consistency, strict tenant isolation, and sub-second calendar operations.

---

## 2. Core Functional Subsystems

### 2.1 Provider Management
- **Organization Onboarding**: Simple multi-step onboarding wizard for salon owners:
  - Business identity: Name, slug, service category (Salon, Barbershop, Beauty Studio, Spa).
  - Launch location: Configurable city (defaulting to Gujarat urban centers), address, operational timezone (`Asia/Kolkata`).
  - Working schedule: Weekly opening/closing hours with split shifts (e.g. morning and evening slots).
- **Single Operational Location MVP**:
  - The business entity binds to a primary `Location` record.
  - The domain schema maintains `LocationId` on all operational records (`Staff`, `ServiceLocation`, `Booking`), ensuring effortless multi-location branching post-MVP without schema refactoring.

### 2.2 Business Ownership & Tenant Boundaries
- **Multi-Tenancy Model**: Logical multi-tenancy sharing a single PostgreSQL database with row-level tenant discrimination (`TenantEntity.TenantId`).
- **Authorization & Context Derivation**:
  - Tenant ID is **never trusted** from request bodies or client headers for provider operations.
  - Tenant ID is securely resolved from the authenticated user's JWT claims (`tenant_id`) and verified against `OrganizationMembership`.
  - Global query filters on `BooklineDbContext` automatically isolate queries:
    $$\text{Filter: } \text{IsSystem} \lor (\text{TenantId} = \text{CurrentTenantId})$$
  - `TenantSaveChangesInterceptor` enforces tenant assignment on all entity insertions and prevents cross-tenant mutations.

### 2.3 Service & Staff Management
- **Service Catalog**:
  - Services defined with category, duration (minutes), buffer-before, and buffer-after times.
  - Prices stored in decimal-safe integer/scaled precision (`decimal(18,2)`) in Indian Rupees (INR).
- **Staff Roster & Scheduling**:
  - Staff profiles assigned to services (`StaffService` junction).
  - Individual working hours overriding or inheriting tenant working hours.
  - Discrete `TimeOff` intervals for lunch breaks, vacations, or sick leave.

### 2.4 Availability & Unified Booking Engine
- **Unified Engine**:
  - Online customer bookings and in-store manual walk-ins pass through the same server-side scheduling rules.
- **Server-Authoritative Slot Calculation (`ISlotEngine`)**:
  - Projects staff working hours against existing appointments, buffers, and time-off windows.
  - Calculated strictly on the server in the provider's local timezone (`DateTimeZone`) using **NodaTime**.
  - Clocks decoupled via `NodaTime.IClock` to ensure deterministic execution and temporal correctness.
- **Concurrency & Double-Booking Protection**:
  1. Distributed short-lived slot hold in Redis (10 minutes) for customer web checkout.
  2. Database GiST exclusion constraint (`no_overlap`) preventing concurrent overlaps:
     ```sql
     ALTER TABLE "Bookings" ADD CONSTRAINT no_overlap
       EXCLUDE USING gist (
         "StaffId" WITH =,
         tstzrange("StartUtc", "EndUtc") WITH &&
       )
       WHERE ("Status" IN ('Pending', 'Confirmed'));
     ```
  3. Optimistic concurrency token (`RowVersion`) on booking entities.
- **Manual Provider Booking (Walk-in / Phone)**:
  - Dedicated provider command (`CreateProviderBookingCommand`) allowing receptionists/owners to directly reserve a slot without requiring a client-side Redis hold, while executing the identical server conflict checks.

### 2.5 Customer Management (CRM)
- **Tenant-Scoped Customer Directory**:
  - Providers own their customer records (`Customer.TenantId`). Customer data is never shared across rival salons.
  - Tracks client details (name, phone, email, notes, total visits, total spend).
  - Walk-in clients without prior accounts are seamlessly captured into the provider's directory upon appointment entry.

### 2.6 Notifications & Transactional Outbox
- **Transactional Outbox Pattern**:
  - Notification events (`BookingCreated`, `BookingRescheduled`, `BookingCancelled`, `ReminderDue`) are written to `OutboxMessages` in the same database transaction as the appointment state change.
  - A resilient background worker (`OutboxAndReminderWorker`) polls unprocessed outbox messages, ensures at-least-once delivery, and records delivery logs.
  - Outbox dispatch failures do NOT roll back or corrupt appointment data.
- **Notification Channels**:
  - Abstracted notification service supporting Email (MailKit / SMTP) and extensible to SMS / WhatsApp providers (e.g. Twilio, Gupshup, Fast2SMS) for Indian phone numbers.

### 2.7 Public Booking Pages
- **Direct Provider Booking Link (`/book/{slug}`)**:
  - Mobile-first, lightweight responsive interface.
  - Clean flow: Service selection $\rightarrow$ Staff selection $\rightarrow$ Real-time slot picker $\rightarrow$ Contact details $\rightarrow$ Instant confirmation.
  - Direct booking links operate without requiring customer account registration; customer record is matched or created via phone/email under the provider's tenant.

### 2.8 Platform Administration
- **System Governance**:
  - PlatformAdmin role for platform maintainers.
  - Provider onboarding inspection, business status verification, and tenant health monitoring.
  - Zero access to customer PII across unrelated tenants.

---

## 3. Architectural Risks & Consequences of Inaction

| Risk Area | Architectural Vulnerability | Consequence If Ignored | Mitigating Target Architecture |
|---|---|---|---|
| **Temporal & Clock Correctness** | Relying on ungrounded `DateTime.Now` or server machine timezone for slot generation. | Appointments shift by $+5.5$ hours; bookings created in UTC bleed across calendar day boundaries; DST errors; hold expiries trigger prematurely. | Mandatory **NodaTime** types (`Instant`, `LocalDate`, `ZonedDateTime`), stored IANA zone (`Asia/Kolkata`), and server-authoritative `IClock`. |
| **Concurrency & Double-Booking** | Relying solely on client-side availability checks or uncoordinated database writes. | Two clients book the same stylist at 2:00 PM simultaneously; salon faces angry walk-ins, reputational damage, and operational chaos. | Server-authoritative `ISlotEngine`, atomic Redis holds (`SET NX EX`), and PostgreSQL GiST exclusion constraints (`no_overlap`). |
| **Tenant Data Leakage** | Trusting client-supplied `TenantId` parameters on API routes. | Malicious or buggy clients query or overwrite another salon's client phone numbers, revenue data, or appointments. | Server-derived tenant claims from JWT; global EF Core query filters; `TenantSaveChangesInterceptor` rejection. |
| **Fragmented Booking Records** | Keeping walk-ins in local React state or a separate offline table while online bookings use another table. | Overbooking when walk-ins aren't reflected online; inaccurate daily revenue metrics; data lost on browser refresh. | Unified `Bookings` table and identical server conflict pipeline for both manual and online appointments. |
| **Over-Engineered Scope Distraction** | Building retail e-commerce carts, inventory tracking, commission splits, and payment gateway escrow before calendar PMF. | Massive maintenance overhead, delayed MVP launch, high payment gateway drop-offs, and lack of core scheduling reliability. | Freeze e-commerce/commission modules; focus 100% on provider SaaS calendar, staff scheduling, and direct booking links. |
| **Monolithic Hardcoded City** | Hardcoding "Ahmedabad" across database defaults and discovery queries. | Inability to launch in Surat, Vadodara, or Rajkot without extensive codebase search-and-replace refactoring. | Config-driven launch city parameter (`AppConfig:LaunchCity`) injected through dependency injection. |

