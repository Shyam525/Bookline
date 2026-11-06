# Bookline Implementation Roadmap & Phased Execution Plan

## 1. Roadmap Architecture & Execution Philosophy

Bookline's implementation follows a disciplined, phase-by-phase engineering methodology. Each phase represents a self-contained domain milestone with clear functional boundaries, automated test coverage, and strict acceptance gates.

```
Phase 0 ──► Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4 ──► Phase 5 ──► Phase 6 ──► Phase 7 ──► Phase 8 ──► Phase 9
Audit &     Domain &    Provider    Services    Authoritative  Unified    Direct      Customer    Outbox &     Production
Scope       Tenant      Onboarding  & Staff     Availability   Calendar   Booking     CRM &       Gujarati     Launch
Freeze      Foundations & Location  Rosters     & Conflicts    Workspace  Link Flow   Offline Pay i18n         Hardening
```

---

## 2. Phase Breakdown & Acceptance Deliverables

### Phase 0: Repository Audit, Architecture Blueprint & Scope Freezing (Current Phase)
- **Goal**: Full codebase inspection, verification of test baseline, inventory of working vs simulated modules, freezing product scope, and architectural documentation.
- **Key Deliverables**:
  - `docs/product/bookline-mvp.md`
  - `docs/architecture/current-state.md`
  - `docs/architecture/target-architecture.md`
  - `docs/planning/implementation-roadmap.md`
  - `docs/quality/acceptance-criteria.md`
  - `docs/validation/provider-interview-guide.md`
  - `docs/validation/customer-interview-guide.md`
  - `docs/validation/pilot-metrics.md`
- **Gate**: Complete codebase audit delivered, scope frozen, zero code regressions, report presented.

---

### Phase 1: Core Domain & Tenant Foundations Alignment
- **Goal**: Resolve test baseline discrepancies, configure launch city dynamically, and establish the server-side provider booking command contract.
- **Key Deliverables**:
  - Fix test harness instantiation in `ProviderOnboardingPhase2Tests` (`_dbContext` dependency).
  - Fix temporal clock dependency in `PublicAvailabilityIntegrationTests` to use dynamic upcoming test dates.
  - Refactor hardcoded "Ahmedabad" city references into configuration-driven parameters (`AppConfig:LaunchCity`).
  - Introduce `CreateProviderBookingCommand` in `Bookline.Application` allowing provider-authenticated appointment creation without requiring client-side Redis holds.
- **Gate**: All 103+ backend tests pass with 0 failures; frontend builds cleanly.

---

### Phase 2: Provider Onboarding, Profile & Location Foundation
- **Goal**: Streamline the provider onboarding experience to single-location operational MVP with Gujarat regional settings.
- **Key Deliverables**:
  - Single-location business profile setup (Name, category, address, phone, timezone default `Asia/Kolkata`).
  - Weekly business operating hours setup with split shifts.
  - Verification of `TenantEntity` global query filters and interceptor integrity.
  - Clean UI with loading, error, and empty states (zero hardcoded mock fallbacks).
- **Gate**: Provider can register, complete onboarding, and persist operating hours to PostgreSQL without errors.

---

### Phase 3: Service Catalog & Staff Management
- **Goal**: Enable full CRUD operations for service catalog and staff scheduling.
- **Key Deliverables**:
  - Service management: categories, service name, duration in minutes, buffer before/after in minutes, decimal-safe price in INR (`₹`).
  - Staff management: name, phone, email, assigned services (`StaffService`), individual working hours, and time-off intervals (`TimeOff`).
  - Clean API endpoints with validation (`FluentValidation`) and proper HTTP status codes.
- **Gate**: Services and staff can be created, edited, and queried with full tenant isolation; staff working hours persist cleanly.

---

### Phase 4: Server-Authoritative Availability & Conflict Engine
- **Goal**: Verify and harden the availability slot calculation engine against real database data and edge cases.
- **Key Deliverables**:
  - Connect `SlotEngine.cs` to real PostgreSQL working hours, buffer windows, and existing bookings.
  - Comprehensive unit and integration tests for buffer times, split shifts, staff holidays, and time zone projections (`Asia/Kolkata`).
  - Verify PostgreSQL GiST exclusion constraint (`no_overlap`) preventing staff double-booking under concurrent load.
- **Gate**: Slot calculation produces mathematically exact available slots; double-booking attempts fail with `SLOT_UNAVAILABLE` or concurrency conflict.

---

### Phase 5: Unified Calendar & Provider Operations Workspace
- **Goal**: Transform the provider calendar into an interactive, real-time operational dashboard connected to the backend API.
- **Key Deliverables**:
  - Replace in-memory appointment creation in `CalendarPage.tsx` and `DashboardPage.tsx` with live API calls (`CreateProviderBookingCommand`).
  - Real appointment status transitions: Check-in, Complete, No-Show, Cancel with backend persistence.
  - Eliminate all hardcoded mock arrays and fake data fallbacks; implement clean empty and loading states.
  - Responsive daily and weekly calendar views optimized for tablet and desktop viewports.
- **Gate**: Walk-in appointment entered on provider calendar persists to PostgreSQL and immediately updates slot availability; status transitions persist across page refreshes.

---

### Phase 6: Direct Provider Public Booking Experience
- **Goal**: Provide an ultra-fast, mobile-optimized booking flow for end customers via direct provider link (`/book/{slug}`).
- **Key Deliverables**:
  - Lightweight public booking flow: Service selection $\rightarrow$ Stylist selection $\rightarrow$ Real-time slot picker $\rightarrow$ Contact details $\rightarrow$ Instant confirmation.
  - Slot hold acquisition (10 minutes) preventing slot race conditions during form completion.
  - Customer contact capture into provider's tenant directory.
  - Public booking confirmation screen with appointment reference code and cancellation policy details.
- **Gate**: End customer completes booking on mobile viewport; appointment appears instantaneously on provider's unified calendar.

---

### Phase 7: Customer CRM & Offline Payment Recording
- **Goal**: Equip providers with client relationship tools and offline payment tracking.
- **Key Deliverables**:
  - Provider customer directory: search by name/phone, view past visit history, lifetime appointments, and personal stylist notes.
  - Offline payment recording on calendar appointment completion: Cash, Direct UPI QR, Card swipe.
  - Decimal-safe revenue summaries on provider dashboard (today's revenue, completed appointments, upcoming bookings) computed dynamically from real database records.
- **Gate**: Provider can search customers, log notes, record UPI/Cash payments upon checkout, and inspect accurate daily totals.

---

### Phase 8: Transactional Outbox, Notifications & Gujarati Localization
- **Goal**: Ensure reliable asynchronous event processing and bilingual accessibility.
- **Key Deliverables**:
  - Implement Transactional Outbox pattern (`OutboxMessages`) for appointment confirmations, rescheduling, and cancellations.
  - Resilient background worker processing outbox events with retry and dead-letter handling.
  - Reviewable Gujarati translation dictionary (`gu.json`) and bilingual language switcher in the frontend.
- **Gate**: Outbox worker reliably processes booking events; UI switches seamlessly between English and Gujarati without layout distortion.

---

### Phase 9: Production Hardening, Verification & Pilot Launch Readiness
- **Goal**: End-to-end regression testing, security review, Docker configuration validation, and pilot deployment readiness.
- **Key Deliverables**:
  - Full automated end-to-end journey tests verifying provider and customer flows.
  - Multi-tenant boundary penetration test verifying zero cross-tenant data leakage.
  - Production Docker Compose configuration with PostgreSQL backup strategy and health check probes.
  - Pilot telemetry and KPI tracking dashboard setup.
- **Gate**: Zero failing tests; zero fake data; successful deployment to staging/pilot environment.

