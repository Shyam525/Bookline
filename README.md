# BOOKLINE — Provider Management & Appointment Platform

**Bookline** is a provider-management SaaS and appointment-booking platform built for independent local service businesses (salons, barbershops, beauty studios, spas, and wellness centers).

- **Initial Market**: Gujarat, India (Configurable launch city, initial focus: Ahmedabad, Surat, Vadodara, Rajkot).
- **Product Strategy**: **Provider-Management SaaS First, Customer Booking & Discovery Second**.
- **The Core Anchor**: **The Unified Calendar**. Walk-ins, phone calls, WhatsApp/Instagram requests, and direct booking links share identical scheduling rules and database records.

---

## Architecture & Phase 0 Documentation Blueprint

The platform architecture, roadmap, and validation frameworks are comprehensively documented under `docs/`:

1. **[MVP Product Specification](docs/product/bookline-mvp.md)**: Frozen product scope, provider/customer promises, and deliberate operational constraints.
2. **[Current State Technical Audit](docs/architecture/current-state.md)**: Comprehensive codebase inventory, test health baseline, and .NET framework assessment.
3. **[Target Architecture Blueprint](docs/architecture/target-architecture.md)**: Modular monolith architecture across all 8 core subsystems and risk mitigations.
4. **[Implementation Roadmap](docs/planning/implementation-roadmap.md)**: Phase-by-phase execution plan from Phase 0 to Phase 9 with strict acceptance gates.
5. **[Acceptance Criteria Framework](docs/quality/acceptance-criteria.md)**: Testable quality standards, temporal correctness, and tenant isolation invariants.
6. **[Provider Interview Guide](docs/validation/provider-interview-guide.md)**: Field discovery guide for salon and barbershop owners in Gujarat.
7. **[Customer Interview Guide](docs/validation/customer-interview-guide.md)**: Customer discovery guide evaluating booking friction and waiting pain.
8. **[Pilot Performance Metrics](docs/validation/pilot-metrics.md)**: Quantitative and qualitative pilot validation KPIs (DACU, zero double-bookings, retention).

---

## 1. Product Model & Actors

### A. Customer (Universal Account)
- Single universal Bookline client account across all providers.
- Real-time location awareness ("📍 Near me", city search: Ahmedabad, Mumbai, Bangalore, Surat, Rajkot).
- Interactive vector map with price tags, marker previews, and "Search this area" sync.
- 5-step appointment scheduling with 5-minute authoritative slot holds, live countdown, and `.ics` calendar exports.
- Multi-vendor retail commerce cart with single-provider fulfillment protection.
- Cross-provider booking roster with rescheduling and cancellation notice policies.
- Verified review submissions tied to completed visits.

### B. Provider / Business Owner (Operations OS)
- Multi-business ownership with fast tenant context switching (e.g., *Aura Wellness & Spa* vs. *Glow Hair Lounge*).
- Operational location branch selector (*Bodakdev Flagship*, *Satellite Branch*, *Mumbai Studio*).
- Day/Week appointment calendar with specialist lanes and status workflows (`Pending`, `Confirmed`, `CheckedIn`, `Completed`, `Cancelled`, `NoShow`).
- Manual appointment creation across controlled sources: `WalkIn`, `Phone`, `WhatsApp`, `Instagram`, `Manual`, and `Online`.
- Price snapshotting and service name snapshotting preserving historical billing integrity against subsequent catalog changes.
- Confidential internal provider notes (isolated from client view) alongside customer notes.
- Shared conflict scheduling engine (`IAppointmentSchedulingService`) enforcing business operating hours, staff shift schedules, breaks, time-offs, double-booking prevention, and past appointment rules.
- Automatic customer deduplication and reuse by normalized phone/email, plus dedicated customer appointment history inspector.
- Physical retail product catalog with stock counts, held reservations, and online toggle.
- Retail order fulfillment pipeline (`Paid` → `Processing` → `Ready` → `Completed`).
- Public storefront profile customization (cover, logo, bio, cancellation window, advance deposit rules).
- Granular team RBAC permissions (*Owner*, *Admin*, *Manager*, *Specialist*, *Receptionist*).

### C. Bookline Platform Admin (Governance Console)
- Platform-wide GMV, volume, and take-rate commission reporting (10% retained take).
- Provider KYC directory with one-click verification and suspension controls.
- Client review moderation queue protecting marketplace authenticity.
- Commission transaction ledger and provider payout disbursement trigger.
- System infrastructure monitors (PostgreSQL, PostGIS GIST indexes, Redis coordinator, Outbox worker).

---

## 2. Technology Stack & Architecture

- **Backend**:
  - ASP.NET Core 10 & C# (.NET 10 LTS)
  - Clean Architecture & Modular Domain-Driven Design
  - Entity Framework Core 10 with PostgreSQL & PostGIS Spatial Extension
  - Redis In-Memory Coordinator (Distributed slot holds & concurrency locks)
  - NodaTime (Temporal precision with IANA timezone boundaries)
  - MediatR CQRS, FluentValidation, Transactional Outbox Pattern
- **Frontend**:
  - React 18, TypeScript, Vite
  - Tailwind CSS with luxury dark aesthetic (`#0A0C13` background, `#111520` surface, `#E8546A` coral accent)
  - Playfair Display & DM Sans typography
  - TanStack Query, React Router, Lucide Icons, Leaflet / Vector Map
- **Testing**:
  - xUnit, FluentAssertions, Architecture tests, Integration harness, Node.js TAP runner, Playwright
  - 628 Automated Tests passing with 0 failures (556 backend + 62 frontend + 10 Playwright E2E)
  - Unit tests for Phase 3: Shared conflict scheduling (`IAppointmentSchedulingService`), manual appointment creation, historical snapshots, customer deduplication, and lifecycle transitions.

---

## 3. Seed Accounts & Credentials (Section 126)

The system is pre-seeded deterministically with realistic demo data across all 8 categories (Beauty, Healthcare, Fitness, Wellness, Photography, Education, Consulting, Home Services) and 5 cities (Ahmedabad, Rajkot, Surat, Mumbai, Bangalore):

| Role | Email | Password | Scope & Responsibilities |
| :--- | :--- | :--- | :--- |
| **Customer** | `customer@bookline.local` | `Customer123!` | Consumer discovery, bookings, cart checkout, profile |
| **Provider** | `provider@bookline.local` | `Provider123!` | Multi-business owner (*Aura Wellness* & *Glow Lounge*), calendar, orders |
| **Platform Admin** | `admin@bookline.local` | `Admin123!` | Marketplace governance, KYC verification, commissions, payouts |

*Note: All demo accounts use development-only passwords.*

---

## 4. Quick Start & Execution (Sections 127 & 128)

### Option A: Docker Compose (One-Command Startup)
```bash
docker compose up --build
```
This launches:
- **bookline-web**: Frontend running on [http://localhost:3000](http://localhost:3000)
- **bookline-api**: Backend REST API running on [http://localhost:5168](http://localhost:5168)
  - Swagger UI: [http://localhost:5168/swagger](http://localhost:5168/swagger)
  - Liveness: [http://localhost:5168/health](http://localhost:5168/health)
  - Readiness: [http://localhost:5168/ready](http://localhost:5168/ready)
  - Observability Telemetry: [http://localhost:5168/api/v1/observability/status](http://localhost:5168/api/v1/observability/status)
- **postgres**: PostgreSQL 16 + PostGIS extension on `localhost:5432`
- **redis**: Redis coordinator for holds and cache on `localhost:6379`
- **mailpit**: Local SMTP relay & Webmail UI on [http://localhost:8026](http://localhost:8026)

#### Docker & WSL2 Networking Architecture
In a Windows/WSL2 hybrid development environment:
- Docker containers run within the Linux kernel in WSL2 and publish ports to `0.0.0.0` inside WSL.
- Windows host connects seamlessly to `localhost:5432` and `localhost:6379` via WSL2 automated localhost forwarding (or via the direct WSL vEthernet adapter IP, e.g. `$(wsl hostname -I).Split(' ')[0]`).
- No undocumented background `socat` proxies or custom Windows `netsh` portproxy rules are required.
- Keep-alive: Ensure WSL2 remains active during long-running background tasks. If WSL idle suspension occurs, keep the VM active or access services directly via the WSL IP.

### Option B: Local Step-by-Step Development

Follow these steps to run Bookline directly on your host machine:

#### 1. Configure Environment Variables
Copy `.env.example` to `.env` or set environment variables in your local environment. Development configurations in `backend/src/Bookline.Api/appsettings.Development.json` use safe local defaults:
```bash
cp .env.example .env
```
> **Security Note**: Never commit production secrets. In production environments (`ASPNETCORE_ENVIRONMENT=Production`), the application enforces validation requiring a cryptographically secure `JwtSettings:Secret` with a minimum length of 32 bytes (256 bits).

#### 2. Start PostgreSQL & Infrastructure Services
Launch PostgreSQL (with PostGIS), Redis, and Mailpit via Docker Compose:
```bash
docker compose up -d postgres redis mailpit
```

#### 3. Apply Database Migrations
Apply the existing EF Core migrations to initialize or update your database schema:
```bash
dotnet ef database update --project backend/src/Bookline.Infrastructure --startup-project backend/src/Bookline.Api
```

#### 4. Start the Backend Web API
Run the ASP.NET Core API server:
```bash
dotnet run --project backend/src/Bookline.Api
```
The API is available at [http://localhost:5168](http://localhost:5168), with Swagger UI at `/swagger`, liveness at `/health`, and readiness at `/ready`.

#### 5. Start the Frontend Application
In a separate terminal, install frontend dependencies and start the Vite development server:
```bash
cd frontend
npm install
npm run dev
```
The client UI will run on [http://localhost:5173](http://localhost:5173) (or [http://localhost:3000](http://localhost:3000) when run via Docker).

#### 6. Run Automated Tests & Verifications
Run the backend and frontend test suites to verify system integrity:
```bash
# Run all backend unit and integration test projects (Domain, Application, Infrastructure, Integration)
dotnet test backend

# Run frontend unit and workflow tests
npm --prefix frontend test

# Run frontend TypeScript type-checking and production build
npm --prefix frontend run build
```

---

## 4.5 Provider Onboarding & Business Configuration (Phase 2)

Phase 2 establishes self-service provider onboarding and runtime business configuration without manual database intervention:

- **Business Profile Subsystem** (`/api/v1/business/profile`):
  - Configuration of business name, public slug, description, category, contact details (phone, email, website), physical address, Gujarat city/area (e.g. `Ahmedabad`, `Bodakdev`), geographical coordinates, and IANA timezone validation (`Asia/Kolkata` via NodaTime Tzdb).
  - Logo, cover, and gallery image persistence.
  - Real-time slug availability endpoint (`/api/v1/business/slug/check?slug=...`) with collision protection.
- **Business Location Model**:
  - Clear primary location modeling (`Location.IsPrimary = true`) operating as one primary location for the MVP while preserving multi-location schema readiness.
- **Business Hours & Booking Policy Subsystem** (`/api/v1/business/policies`):
  - Weekly opening hours with break intervals and closed day support.
  - Lead-time rules: Minimum notice hours (`MinimumNoticeHours`), booking horizon days (`BookingHorizonDays`).
  - Cancellation & rescheduling policies: Window hours (`CancellationWindowHours`, `ReschedulingWindowHours`) and policy disclosures.
- **Staff Management & Break Interval Splitting** (`/api/v1/staff`):
  - Staff creation, editing, and activation/deactivation (`PATCH /api/v1/staff/{id}/status`). Inactive staff are strictly barred from slot holds and appointments.
  - Service assignments with relational integrity validation.
  - Working hours with non-overlapping break validation (`SetWorkingHoursCommandValidator`); automatically splits shift into operational slots avoiding break conflicts.
  - Time-off recording and scheduling exclusion.
- **Provider Setup Wizard & Management UI**:
  - Multi-step onboarding pipeline (`/onboarding`) with draft progress persistence (save and resume).
  - Storefront settings editor (`/provider/settings/storefront`) and Staff availability editor (`/provider/availability`).

---

## 5. Verification & Key Flows

1. **Customer Discovery & Interactive Map**:
   - Navigate to `http://localhost:5173/discover`
   - Select city or click "📍 Near me"
   - Filter by category (*Wellness*, *Beauty*, *Healthcare*, *Fitness*)
   - Pan map and click "Search this area"
2. **Provider Storefront & Step-by-Step Booking**:
   - Open `/business/aura-wellness` or `/business/aura-wellness-ahmedabad` (or `/business/glow-studio`)
   - Select a treatment service -> Select Specialist -> Choose Date & Available Slot
   - Experience the authoritative 5-minute slot hold countdown
   - Confirm booking to receive booking reference `BL-XXXXXX` and export `.ics` calendar event
3. **Retail Commerce & Single-Provider Cart**:
   - Add boutique items to the slide-over cart drawer
   - Proceed to `/checkout` with 10% transparent platform fee
   - Place order and review in `/orders`
4. **Provider Operations OS**:
   - Navigate to `/provider/dashboard`
   - Switch active business between *Aura Wellness* and *Glow Lounge*
   - Manage appointments roster in `/provider/bookings` and update arrival check-ins
   - Manage retail stock levels in `/provider/products` and fulfill orders in `/provider/orders`
5. **Platform Admin Governance**:
   - Navigate to `/admin`
   - Verify or suspend providers in `/admin/providers`
   - Moderate client feedback in `/admin/reviews`
   - Review platform commission ledger and disburse provider payouts in `/admin/payments`

---

## 6. Final Deliverable & Verification Report (Section 157)

- **Deliverable Archive**: `BOOKLINE-final.zip` (located in repository root).
  - Clean production archive containing pristine source code, configurations, test suites, and documentation.
  - Zero temporary files, build outputs (`bin`/`obj`), `node_modules`, or `.git` trees.
- **Exact Startup Command**:
  ```bash
  docker compose up --build
  ```
- **Exact Application URLs**:
  - Web Application: [http://localhost:3000](http://localhost:3000)
  - Backend API & Swagger: [http://localhost:5168/swagger](http://localhost:5168/swagger)
  - Health Endpoint: [http://localhost:5168/health](http://localhost:5168/health)
  - Readiness Endpoint: [http://localhost:5168/ready](http://localhost:5168/ready)
  - Mailpit UI: [http://localhost:8026](http://localhost:8026)
- **Demo Credentials** (Development-only passwords):
  - **Customer**: `customer@bookline.local` / `Customer123!`
  - **Provider**: `provider@bookline.local` / `Provider123!`
  - **Platform Admin**: `admin@bookline.local` / `Admin123!`
- **Exact Test Commands**:
  ```powershell
  # Backend Tests (513 passing)
  dotnet test backend

  # Frontend Tests (49 passing)
  npm --prefix frontend test
  ```
- **Verification Report**:
  ```text
  Passed!  - Failed: 0, Passed:  21, Skipped: 0, Total:  21 - Bookline.Domain.UnitTests.dll (net8.0)
  Passed!  - Failed: 0, Passed: 413, Skipped: 0, Total: 413 - Bookline.Application.UnitTests.dll (net8.0)
  Passed!  - Failed: 0, Passed:  63, Skipped: 0, Total:  63 - Bookline.Infrastructure.UnitTests.dll (net8.0)
  Passed!  - Failed: 0, Passed:  16, Skipped: 0, Total:  16 - Bookline.IntegrationTests.dll (net8.0)
  Frontend: 49 passed, 0 failed, 3 suites (Node.js test runner)
  Total: 562 Passed, 0 Failed, 0 Skipped (100% Pass Rate)
  ```

---

## 7. Known Genuine Limitations (Section 149 & 157)

1. **Third-Party Payment Gateways**: In offline development environments where live Stripe secret keys are unconfigured, `StripePaymentProvider` explicitly returns `"Payments not configured."` (per Section 149 No Fake Integration Rule). Local test checkouts use `LocalDemoPaymentProvider` or In-Store POS mode.
2. **External Map Tiles**: OpenStreetMap cartographic tile rendering requires outbound network access. When offline, provider discovery and coordinate markers remain 100% interactive using the vector coordinate fallback layer.
3. **Email Relay**: In development, notifications route to the embedded local Mailpit SMTP relay at [http://localhost:8026](http://localhost:8026) without external SMTP relay dependencies.

