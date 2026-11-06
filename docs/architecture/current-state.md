# Bookline Current-State Technical Audit & Codebase Inventory

## 1. Executive Summary & Verification Baseline

This document provides a comprehensive audit of the Bookline repository as of November 2026. The inspection was conducted using direct static analysis, compilation runs, test runner executions, and dependency checks.

### Current Build & Test Health Matrix

| Project / Component | Target Framework / Runtime | Status | Automated Test Result | Critical Observations |
|---|---|---|---|---|
| **`Bookline.Api`** | .NET 8 (`net8.0`) | **Builds Cleanly** | N/A (Host API) | Compiles with 0 warnings/errors under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. |
| **`Bookline.Application`** | .NET 8 (`net8.0`) | **Builds Cleanly** | N/A (Core Use Cases) | Houses MediatR commands, queries, and `SlotEngine`. |
| **`Bookline.Domain`** | .NET 8 (`net8.0`) | **Builds Cleanly** | **21 / 21 PASS (100%)** | Zero NuGet references. Pure domain model. All domain state machine tests passing. |
| **`Bookline.Infrastructure`** | .NET 8 (`net8.0`) | **Builds Cleanly** | **63 / 63 PASS (100%)** | Persistence, PostGIS spatial queries, Redis hold service, MailKit adapters pass cleanly. |
| **`Bookline.Application.UnitTests`** | .NET 8 (`net8.0`) | **Builds Cleanly** | **359 PASS / 3 FAIL** | 3 tests failed in `ProviderOnboardingPhase2Tests` due to `_dbContext` null-check in `OnboardingController.GetStatus`. |
| **`Bookline.IntegrationTests`** | .NET 8 (`net8.0`) | **Builds Cleanly** | **15 PASS / 1 FAIL** | 1 test failed (`PublicAvailabilityIntegrationTests`) due to hardcoded past test date (`2026-10-30`) violating current time check. |
| **`frontend` (TypeScript & React)** | Vite 5.4, React 18.3, TS 5.6 | **Builds Cleanly** | **49 / 49 PASS (100%)** | Production bundle builds cleanly (`1,726 modules transformed`, zero TypeScript errors). Node test runner passes all 49 tests. |

---

## 2. Codebase Architecture & Project Inventory

The repository is structured as a Clean Architecture Modular Monolith with the following directory layout:

```
Bookline/
├── backend/
│   ├── Bookline.slnx                      # Root solution file
│   ├── Dockerfile                         # Multi-stage .NET 8 build & runtime
│   ├── src/
│   │   ├── Bookline.Api/                  # ASP.NET Core Web API host
│   │   ├── Bookline.Application/          # Application use cases, commands, queries, SlotEngine
│   │   ├── Bookline.Domain/               # Pure domain entities, value objects, enums
│   │   ├── Bookline.Infrastructure/       # EF Core, PostgreSQL, PostGIS, Redis, MailKit, Hangfire
│   │   └── Bookline.Worker/               # Background host for outbox processing & reminders
│   └── tests/
│       ├── Bookline.Domain.UnitTests/
│       ├── Bookline.Application.UnitTests/
│       ├── Bookline.Infrastructure.UnitTests/
│       └── Bookline.IntegrationTests/
├── frontend/
│   ├── package.json                       # React 18, Vite 5, Tailwind 3.4, TanStack Query 5
│   ├── tsconfig.json                      # Strict TypeScript compilation
│   ├── Dockerfile                         # Node build + Nginx Alpine runtime
│   └── src/
│       ├── app/                           # Auth & Query providers, Router
│       ├── components/                    # UI primitives, forms, scheduling, maps, feedback
│       ├── pages/                         # App (Provider), Customer, Admin, Auth, Marketing
│       ├── services/api/                  # Axios/fetch API clients and canonical endpoints
│       └── utils/                         # Money, timezone, validation, error helpers
├── docs/                                  # Architectural and technical documentation
└── scripts/                               # Cross-platform development, test, and run scripts
```

---

## 3. Deep-Dive Component Audit: Real vs. Simulated vs. Broken

### 3.1 Persistence & Database Layer
- **Configuration**: PostgreSQL 16 + PostGIS configured in `docker-compose.yml` (`postgis/postgis:16-3.4-alpine`).
- **Connection Logic**: `Program.cs` intelligently tests TCP port 5432; if local PostgreSQL is unreachable, it logs a warning and falls back to an EF Core In-Memory database for local development.
- **EF Core Migrations**: Located in `backend/src/Bookline.Infrastructure/Persistence/Migrations`:
  1. `20261010162453_InitialPostgreSqlSchema`
  2. `20261014110941_Phase1_DomainBoundariesAndTenantFoundations`
  3. `20261016183846_Section85QueryDrivenIndexes`
  4. `20261016190522_Section86FinancialCorrectnessSnapshots`
- **Concurrency & Invariants**:
  - PostgreSQL GiST exclusion constraint (`no_overlap`) preventing staff appointment overlap for `Pending` and `Confirmed` statuses.
  - Optimistic concurrency control via `RowVersion` byte array on `Booking` and `Product`.

### 3.2 Time & Scheduling Engine
- **NodaTime Integration**: Solid architecture using `Instant`, `LocalDate`, `LocalTime`, and `DateTimeZone`.
- **Authoritative Server Engine**: `SlotEngine.cs` correctly calculates candidate slots within working hours, subtracting breaks, time-offs, existing bookings, and applying `bufferBefore` and `bufferAfter` duration windows.
- **Clock Abstraction**: `NodaTime.IClock` injected across services, allowing deterministic unit testing via simulated test clocks.

### 3.3 Multi-Tenancy & Authorization
- **Tenant Isolation**: All tenant entities implement `TenantEntity` with `TenantId`.
- **Global Query Filter**: Automatically injected on `BooklineDbContext`:
  `e => _tenantContext.IsSystem || e.TenantId == _tenantContext.TenantId`
- **Tenant Context Resolution**: `TenantContextMiddleware` extracts `tenant_id` claim from JWT tokens. Authenticated customer accounts cannot escalate tenant IDs on mutating requests.

---

## 4. Identified Discrepancies, Architectural Risks & Gaps

### 4.1 Incomplete Provider Appointment Creation Workflow (Unified Calendar Gap)
- **Problem**: `BookingsController.cs` has endpoints to inspect calendar intervals (`GET /api/v1/bookings`) and transition appointment statuses (`/cancel`, `/confirm`, `/check-in`, `/complete`, `/no-show`), but **has NO endpoint for a provider to create an appointment**.
- **Public Booking Limitation**: The only booking creation endpoint is `POST /api/v1/public/book` in `PublicBookingsController.cs`, which strictly requires a Redis slot hold (`HoldId`).
- **Frontend Workaround**: In `frontend/src/pages/app/CalendarPage.tsx` and `DashboardPage.tsx`, when a provider creates a walk-in or quick appointment, the frontend creates a temporary object in React local state (`id: 'bk-' + Date.now()`) and prepends it to the local array. **It never calls the backend API!**
- **Impact**: Refreshing the browser deletes the appointment. Manual and online appointments do not share a single source of truth in the database.

### 4.2 Over-Engineered Scope vs. MVP Constraints
The codebase currently contains extensive features from an earlier e-commerce marketplace build:
- Physical merchandise entities (`Product`, `ProductCategory`, `ProductImage`, `Order`, `OrderItem`).
- Multi-vendor single-provider cart and retail checkout (`OrdersController.cs`, `ordersApi.ts`).
- Platform commission calculations and provider payout balances (`Commission.cs`, `Payout.cs`, `PayoutProvider.cs`).
- Stripe and Razorpay payment gateway integration stubs (`PaymentsController.cs`).

*Resolution Strategy*: Preserve working code without breaking existing builds, but freeze these surfaces, remove them from active provider navigation, and refocus development entirely on the core service-scheduling MVP.

### 4.3 Hardcoded Fallback Mock Data in Frontend
- When the backend returns an empty array for staff, services, or bookings, `CalendarPage.tsx`, `DashboardPage.tsx`, `ProviderBookingsPage.tsx`, and `CustomersPage.tsx` fall back to hardcoded mock records ("Priya Sharma", "Glow Studio", "Aarav Patel").
- **Consequence**: Providers cannot tell whether their database is connected or empty, violating the "No fake business data" principle. Clean loading, empty, and error states must replace these mock fallbacks.

### 4.4 Hardcoded Gujarat City ("Ahmedabad")
- "Ahmedabad" is hardcoded into `DiscoveryController.cs`, `GeoController.cs`, `OnboardingController.cs`, and `LocationDtos.cs`.
- **Consequence**: The launch city is not configurable, violating the MVP product constraint.

### 4.5 Absence of Gujarati Frontend Localization
- The frontend has zero i18n setup (`react-i18next` or lightweight translation context is absent). All text is English.
- **Consequence**: Providers and salon assistants in Gujarat cannot use the system in their native language.

### 4.6 Automated Test Failures
1. `ProviderOnboardingPhase2Tests` (3 tests failing):
   - Controller tests invoke `OnboardingController.GetStatus()`.
   - The method performs `if (callerId == Guid.Empty || _dbContext == null) return Unauthorized();`.
   - The test fixture instantiated `OnboardingController` with a 3-argument constructor where `_dbContext` defaulted to `null`, causing an unexpected `401 Unauthorized` instead of `403 Forbidden` / `200 OK`.
2. `PublicAvailabilityIntegrationTests` (1 test failing):
   - Querying availability for `date=2026-10-30`.
   - The server clock evaluated `2026-10-30` as a past date relative to current time (`2026-11-06`), correctly returning 0 slots, which caused `Assert.NotEmpty(slots)` to fail.

---

## 5. .NET Target Framework Assessment (.NET 8 vs .NET 10 LTS)

### Context & Environment
- Current repository target framework: **.NET 8 (`net8.0`)**.
- Local host runtime SDK: **.NET 10 (`10.0.401`)**.
- `Directory.Build.props` has `<RollForward>LatestMajor</RollForward>`, allowing .NET 8 binaries to build and run seamlessly under the .NET 10 SDK with zero warnings or errors.

### Upgrade Trade-Off Analysis

| Criteria | Retaining .NET 8 (`net8.0`) | Upgrading to .NET 10 (`net10.0`) |
|---|---|---|
| **LTS Support Window** | .NET 8 is an active Long Term Support (LTS) release supported by Microsoft through November 2026. | .NET 10 LTS is the current active LTS cycle supported through late 2028. |
| **Package Compatibility** | All 14 NuGet dependencies (EF Core 8.0.11, Npgsql 8.0.11, Hangfire 1.8.17, MailKit 4.10, NodaTime 3.2) are 100% verified and stable. | Requires upgrading EF Core, Npgsql, ASP.NET packages to 10.0. Potential breaking changes in Npgsql date mapping, spatial types, or middleware contracts. |
| **Container Images** | `mcr.microsoft.com/dotnet/sdk:8.0` and `aspnet:8.0` are standard, lightweight, and universally cached. | Requires updating Dockerfiles to .NET 10 image tags. |
| **Execution Risk** | **Zero Risk**: Code compiles cleanly with 0 errors and 0 warnings today. | **Moderate Risk**: High likelihood of transient package incompatibilities and migration churn during MVP foundation. |

### Architectural Recommendation
**Retain .NET 8 (`net8.0`) for active MVP development while running on the .NET 10 SDK via `RollForward`.** This yields the performance benefits of the .NET 10 JIT compiler and runtime while avoiding destabilizing package upgrades and breaking schema changes during core MVP stabilization. Plan an explicit framework upgrade phase once the provider calendar and booking engine acceptance gates are achieved.

