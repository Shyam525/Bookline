# BOOKLINE — Elite Production-Grade Multi-Vendor Marketplace Platform

**Bookline** is a real, scalable, multi-vendor marketplace platform where:
- **Customers** discover local businesses, services, and products with interactive vector maps and PostGIS geolocation.
- **Providers** operate multi-business organizations on Bookline (storefronts, branch locations, treatment menus, retail inventory, appointments calendar, and payouts).
- **Platform Administrators** govern discovery, KYC verification, review moderation, transactional take-rate economics (10%), and infrastructure health.

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
  - ASP.NET Core 8 & C# (.NET 8.0)
  - Clean Architecture & Modular Domain-Driven Design
  - Entity Framework Core 8 with PostgreSQL & PostGIS Spatial Extension
  - Redis In-Memory Coordinator (Distributed slot holds & concurrency locks)
  - NodaTime (Temporal precision with IANA timezone boundaries)
  - MediatR CQRS, FluentValidation, Transactional Outbox Pattern
- **Frontend**:
  - React 18, TypeScript, Vite
  - Tailwind CSS with luxury dark aesthetic (`#0A0C13` background, `#111520` surface, `#E8546A` coral accent)
  - Playfair Display & DM Sans typography
  - TanStack Query, React Router, Lucide Icons, Leaflet / Vector Map
- **Testing**:
  - xUnit, FluentAssertions, Architecture tests, Integration harness
  - 103 Automated Tests passing with 0 failures

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

### Option B: Local Development
1. **Run Backend**:
   ```bash
   cd backend/src/Bookline.Api
   dotnet run
   ```
2. **Run Frontend**:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
3. **Run Test Suite**:
   ```bash
   dotnet test backend
   ```

---

## 5. Verification & Key Flows

1. **Customer Discovery & Interactive Map**:
   - Navigate to `http://localhost:5173/discover`
   - Select city or click "📍 Near me"
   - Filter by category (*Wellness*, *Beauty*, *Healthcare*, *Fitness*)
   - Pan map and click "Search this area"
2. **Provider Storefront & Step-by-Step Booking**:
   - Open `/business/aura-wellness-ahmedabad`
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
