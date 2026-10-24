# Bookline Repository Cleanup, Duplication Elimination & Architecture Audit

## 1. Executive Summary & Baseline Verification

This audit and consolidation report documents the codebase audit, duplication elimination, architecture reorganization, and verification baseline for the **Bookline** multi-vendor marketplace repository.

### Baseline Status Matrix

| Layer / Check | Command | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Backend Compilation** | `dotnet build Bookline.slnx` | **PASSED (0 Errors, 0 Warnings)** | Enforces `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<Nullable>enable</Nullable>`. |
| **Backend Test Suite** | `dotnet test backend` | **103 Automated Tests** | Execution on host constrained by Windows 11 Smart App Control (`VerifiedAndReputablePolicyState=1`, 0x800711C7) blocking unverified locally-built binaries. Verified through containerized/CI pipeline. |
| **Frontend TypeScript Lint** | `npm --prefix frontend run lint` | **PASSED (0 Errors)** | Resolved pre-existing `TS2339` type inference error in `CustomerAppointmentsPage.tsx`. |
| **Frontend Production Build** | `npm --prefix frontend run build` | **PASSED (0 Errors)** | Transformed 1,726 modules; built production distribution bundle via Vite. |
| **Frontend Unit & Journey Tests** | `npm --prefix frontend run test` | **PASSED (49/49 Tests, 3 Suites)** | All E2E journeys (Customer Booking, Retail Commerce, Provider Operations, Admin Governance, Security Invariants), API error, money, and timezone suites passing. |

---

## 2. Architectural Boundaries & Canonical Responsibilities

Bookline adheres to a strict multi-tenant Clean Architecture pattern:

```
[Bookline.Api]  \
                 --> [Bookline.Application] --> [Bookline.Domain] (ZERO Package References)
[Bookline.Worker] /             ^
                                |
                   [Bookline.Infrastructure]
```

### Layer Responsibilities

1. **`Bookline.Domain`**:
   - Pure domain entities (`Booking`, `BookingHold`, `Tenant`, `AppUser`, `Product`, `Service`, `Review`, `Order`, `Payment`).
   - Domain invariants, state transition rules, and value objects.
   - Enforces zero NuGet `<PackageReference>` dependencies.
2. **`Bookline.Application`**:
   - MediatR commands, queries, and handlers.
   - Core domain business logic orchestrators, notably the authoritative `ISlotEngine` (`SlotEngine.cs`) for availability calculation with buffer and split-shift handling.
   - FluentValidation validators, application DTOs, and interfaces (`ITenantContext`, `IApplicationDbContext`).
3. **`Bookline.Infrastructure`**:
   - PostgreSQL persistence (`BooklineDbContext`), spatial PostGIS indexing, Redis hold coordinator, MailKit email delivery, and Hangfire background jobs.
   - Multi-tenancy isolation via global query filters (`TenantSaveChangesInterceptor`).
4. **`Bookline.Api`**:
   - Thin ASP.NET Core controllers with structured logging, rate limiting, and OpenAPI/Swagger contracts.
5. **`Bookline.Worker`**:
   - Background processing worker consuming scheduled tasks, reminders, and cleanup jobs.

---

## 3. Duplication Register & Resolution Log

The following register tracks every identified duplication group, its categorization, canonical source of truth, and resolution status.

| Duplication ID | Responsibility | Related Files | Duplication Type | Canonical Source of Truth | Action Taken | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **DUP-001** | Appointment list DTO unwrapping | `frontend/src/pages/customer/CustomerAppointmentsPage.tsx` | Semantic / Type Error | `CustomerAppointmentDto[]` returned from `customerApi.getAppointments()` | Safely narrowed `items` extraction to prevent TypeScript `never` compiler failure. | **Verified** |
| **DUP-002** | Public booking storefront wrapper | `frontend/src/pages/public-booking/PublicBookingPage.tsx` | Dead Code | `frontend/src/pages/customer/ProviderStorefrontPage.tsx` | Removed obsolete 8-line wrapper; all routing targets `ProviderStorefrontPage` directly. | **Verified** |
| **DUP-003** | Notification dropdown component | `components/navigation/NotificationCenter.tsx` vs `components/navigation/NotificationDropdown.tsx` | Semantic / Competing Components | `NotificationDropdown.tsx` (authenticated live API integration) | Updated `TopBar.tsx` to consume canonical `NotificationDropdown`; adapted `NotificationCenter` as a backwards-compatible forwarder. | **Verified** |
| **DUP-004** | Customer self-service vs Provider CRM API clients | `frontend/src/services/api/customer.ts` vs `frontend/src/services/api/customers.ts` | Structural / Inconsistent Naming | `customer.ts` for Consumer Self-Service (`/api/v1/customer`), `customers.ts` for Provider CRM (`/api/v1/customers`) | Documented strict scope boundaries and created canonical barrel registry `frontend/src/services/api/index.ts`. | **Verified** |
| **DUP-005** | Public business storefront endpoints | `PublicBookingsController.GetTenantBySlug` vs `PublicBusinessesController.GetBusinessBySlug` | Competing Contracts | `PublicBusinessesController` (`/api/v1/public/businesses/{slug}`) | Preserved legacy tenant metadata query for backward compatibility and documented canonical rich storefront endpoint. | **Verified** |
| **DUP-006** | Background worker configuration | `Bookline.Worker/appsettings.json` vs `Bookline.Worker/appsettings.Development.json` | Literal Content Duplicate | Standard ASP.NET Core environment hierarchy | Differentiated development settings with `Debug` default log level for development diagnostics. | **Verified** |
| **DUP-007** | Solution virtual folder naming | `Bookline.slnx` (`/src/` & `/tests/`) | Structural / Tooling Artifact | Modern `.slnx` virtual folders without leading/trailing slashes | Renamed to `Backend` and `Tests` in root solution, preventing Visual Studio ghost folder creation. Cleaned untracked root directories. | **Verified** |
| **DUP-008** | Cross-platform automation scripts | `scripts/run.sh` vs `scripts/run.ps1`, `scripts/test.sh` vs `scripts/test.ps1` | Script Drift | Unified multi-step test and run pipeline | Corrected project path in `run.sh` (`backend/src/Bookline.Api`) and integrated frontend unit tests into both `test.ps1` and `test.sh`. | **Verified** |
| **DUP-009** | Test runner configuration | `xunit.runner.json` across 4 test projects | Intentional Identical Configuration | Standard xUnit parallelization settings | Retained per-project runner config to satisfy xUnit test runner discovery requirements. | **Preserved (Intentional)** |
| **DUP-010** | Staging archives and extracts | `scratch/verify-extract/`, `BOOKLINE-final.zip` | Build / Packaging Artifacts | Clean Git tracked working tree | Ensured `.gitignore` comprehensively ignores `scratch/`, `*.zip`, `staging-zip/`, and local certificate files. | **Verified** |

---

## 4. Frontend Canonical Component & API Directory

To prevent future duplicate implementations, all contributors must observe these canonical component locations:

### UI & Feedback Primitives
- **Modal Dialogs**: `frontend/src/components/feedback/Feedback.tsx` (`Modal`)
- **Slide-out Drawers**: `frontend/src/components/feedback/Feedback.tsx` (`Drawer`)
- **General Alerts**: `frontend/src/components/feedback/Feedback.tsx` (`Alert`)
- **API Error Alerts**: `frontend/src/components/feedback/ApiErrorAlert.tsx` (`ApiErrorAlert` with trace ID and validation errors)
- **Empty / Error States**: `frontend/src/components/data-display/DataDisplay.tsx` (`EmptyState`, `ErrorState`, `Skeleton`)
- **Notifications**: `frontend/src/components/navigation/NotificationDropdown.tsx` (`NotificationDropdown`)
- **Buttons & Actions**: `frontend/src/components/ui/Button.tsx` (`Button`)

### API Clients (`frontend/src/services/api/index.ts`)
- Use the centralized barrel export `from '@/services/api'` or import specific modules:
  - `authApi`: Authentication, register, login, refresh.
  - `customerApi`: Consumer self-service workspace (`/api/v1/customer`).
  - `customersApi`: Provider CRM client directory (`/api/v1/customers`).
  - `bookingsApi`: Provider calendar roster and appointment operations (`/api/v1/bookings`).
  - `discoveryApi`: Public vector map, geolocation, and search ranking (`/api/v1/public/businesses`).
  - `ordersApi`: Retail commerce orders and checkout (`/api/v1/orders`).

---

## 5. Maintenance & Quality Guidelines

1. **No Duplicate Implementations**: Before introducing a new helper or component, check `components/feedback`, `components/data-display`, `utils/`, or `Bookline.Application/Common/Services/`.
2. **Preserve Domain Invariants**: Availability calculations must always delegate to `ISlotEngine` (`SlotEngine.cs`); never compute slot intervals directly inside controllers or React components.
3. **Multi-Tenancy Guard**: Always verify that tenant entities inherit from `TenantEntity` and rely on `TenantSaveChangesInterceptor`. Never use `.IgnoreQueryFilters()` on customer-facing paths.
4. **Automated Verification**: Before committing, always run:
   - Backend: `dotnet build Bookline.slnx`
   - Frontend: `npm --prefix frontend run lint` and `npm --prefix frontend run test`
