# Bookline Quality Assurance & Acceptance Criteria Framework

## 1. Quality Philosophy

Bookline enforces rigorous, non-negotiable engineering standards. Quality is verified through automated assertions, deterministic tests, architectural static analysis, and explicit acceptance gates rather than manual assumptions.

---

## 2. Global Acceptance Gates (Applicable to All Phases)

### 2.1 Domain & Data Integrity
- [ ] **PostgreSQL as Source of Truth**: All operational entities must persist to PostgreSQL with relational integrity, foreign keys, not-null constraints, and appropriate indexes.
- [ ] **Decimal-Safe Financials**: All prices, totals, and balances must use `decimal(18,2)` with zero floating-point arithmetic drift.
- [ ] **Migration Discipline**: Every schema modification must be backed by an EF Core migration. No manual undocumented database mutations.
- [ ] **Zero Unhandled Exceptions**: All API endpoints must format uncaught failures using the standard `ApiErrorResponse` schema with `code`, `message`, `traceId`, and `status`.

### 2.2 Time & Scheduling Correctness
- [ ] **NodaTime Exclusivity**: No bare `DateTime.Now` or `DateTime.UtcNow` in domain logic or slot calculations.
- [ ] **Authoritative Local Timezone**: Calculations must project into the provider's stored IANA timezone (`Asia/Kolkata`).
- [ ] **Buffer Enforcement**: Slot calculation must strictly account for `BufferBeforeMinutes` and `BufferAfterMinutes` without slot bleeding across operating hour bounds.
- [ ] **Deterministic Testing**: Temporal tests must use injected `NodaTime.IClock` / `TestClock` fixtures rather than machine wall clocks.

### 2.3 Multi-Tenant Isolation & Security
- [ ] **Server-Derived Identity**: The API must never trust a client-supplied `TenantId` on mutating requests. Tenant ID must be derived from validated JWT claims.
- [ ] **Global Query Filters Active**: All tenant-scoped entities must inherit from `TenantEntity` and have EF Core global query filters applied.
- [ ] **Cross-Tenant Test Coverage**: Automated tests must attempt cross-tenant reads and mutations (accessing Tenant A's customer, appointment, or staff using Tenant B's credentials) and assert `403 Forbidden` or `404 Not Found`.

### 2.4 Frontend UX & Production Hygiene
- [ ] **Zero Fake / Hardcoded Business Data**: Dashboards, calendars, and tables must NEVER display hardcoded fallback arrays when data is empty.
- [ ] **Mandatory States**: Every data-driven screen must implement:
  1. *Loading State*: Clear spinner or skeleton layout.
  2. *Error State*: User-friendly error alert with retry button.
  3. *Empty State*: Helpful, encouraging empty state with a call-to-action to create the first record.
  4. *Success Feedback*: Toast or visual confirmation upon mutation.
- [ ] **No Inert / Fake Controls**: Every button, dropdown, and tab must be wired to real functionality or cleanly omitted.
- [ ] **Responsive & Mobile Viewport**: All customer booking flows and provider calendar views must function smoothly on mobile viewports ($375\text{px}$ width) and tablet screens.

---

## 3. Phase-Specific Acceptance Criteria

### Phase 1: Core Domain & Tenant Foundations Alignment
- [ ] All 3 failing tests in `ProviderOnboardingPhase2Tests` resolved by ensuring controller dependencies are properly wired.
- [ ] `PublicAvailabilityIntegrationTests` resolved by dynamically computing upcoming test dates relative to `IClock`.
- [ ] Launch city parameter configured via `appsettings.json` (`AppConfig:LaunchCity`) with default fallback to `"Ahmedabad"` without hardcoded SQL or controller defaults.
- [ ] Application command `CreateProviderBookingCommand` introduced with unit tests verifying validation and conflict rules.

### Phase 2: Provider Onboarding, Profile & Location Foundation
- [ ] Provider registration flow creates an `AppUser`, `Tenant`, and primary `Location` in a single atomic transaction.
- [ ] Provider can configure weekly working hours with support for split shifts (e.g. 10:00 AM - 1:00 PM and 4:00 PM - 9:00 PM).
- [ ] Onboarding wizard progress persists to PostgreSQL; reloading page restores current onboarding step.

### Phase 3: Service Catalog & Staff Management
- [ ] Provider can create, update, and archive service categories and services.
- [ ] Duration and buffer minutes strictly enforced ($> 0$, divisible by slot interval).
- [ ] Provider can add staff members and assign them to specific services.
- [ ] Staff individual working hours and `TimeOff` blocks persist to database and are queryable by date range.

### Phase 4: Server-Authoritative Availability & Conflict Engine
- [ ] Availability query (`GET /api/v1/public/availability`) returns exact valid slots considering staff working hours, breaks, buffer windows, and existing bookings.
- [ ] Slots in the past (prior to current instant) are strictly excluded.
- [ ] Concurrent slot hold requests for the same slot allow exactly one hold; subsequent requests fail with `SLOT_UNAVAILABLE`.
- [ ] PostgreSQL exclusion constraint (`no_overlap`) rejects overlapping appointments at the database engine level.

### Phase 5: Unified Calendar & Provider Operations Workspace
- [ ] Provider can create an appointment directly on the calendar for a walk-in or phone customer; appointment immediately persists to PostgreSQL.
- [ ] Status transitions (`CheckedIn`, `Completed`, `Cancelled`, `NoShow`) execute real API calls and update database state.
- [ ] No hardcoded mock appointments remain in `CalendarPage.tsx`, `DashboardPage.tsx`, or `ProviderBookingsPage.tsx`.
- [ ] Empty state displayed when no appointments exist for the selected date.

### Phase 6: Direct Provider Public Booking Experience
- [ ] Direct booking URL `/book/{slug}` loads provider's public profile and active service catalog without requiring customer authentication.
- [ ] Customer can select service, staff, date, and available slot.
- [ ] System acquires 10-minute hold; client receives temporary reservation token.
- [ ] Submitting contact information confirms appointment and releases hold into a confirmed booking.
- [ ] Appointment instantly appears on the provider's calendar.

### Phase 7: Customer CRM & Offline Payment Recording
- [ ] Creating an appointment for a new customer automatically creates a tenant-scoped `Customer` record.
- [ ] Provider can search customer directory by name or phone number.
- [ ] Provider can mark an appointment as completed and record the offline payment method (Cash, UPI, Card).
- [ ] Dashboard metrics (Today's Appointments, Revenue, New Customers) are computed dynamically from real database records.

### Phase 8: Transactional Outbox, Notifications & Gujarati Localization
- [ ] Outbox message created atomically whenever an appointment is booked, rescheduled, or cancelled.
- [ ] Background worker polls and processes outbox messages with exponential backoff on transient failures.
- [ ] Language toggle allows switching between English and Gujarati.
- [ ] Gujarati translation keys verified by human review for grammatical accuracy and domain fit.

### Phase 9: Production Hardening, Verification & Pilot Launch Readiness
- [ ] Full end-to-end test suite passes cleanly on CI.
- [ ] Docker Compose boots all services (`bookline-api`, `bookline-web`, `postgres`, `redis`) with green health checks (`/health`, `/ready`).
- [ ] Pilot metrics dashboard displays genuine operational statistics.

