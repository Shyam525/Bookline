# BOOKLINE: GEO-DISCOVERY + MARKETPLACE SEARCH + MAPS + STOREFRONTS
## Phase 0: Existing Repository Audit & Implementation Plan

**Audit Timestamp**: 2026-10-12  
**Baseline Test Status**: 152 passed, 0 failed, 0 skipped (100% pass rate)  
**Baseline Build Status**: Clean (0 warnings, 0 errors)  
**Target Solution**: `backend/Bookline.slnx` & `frontend/`

---

### 1. Executive Summary

This audit establishes the baseline technical state of Bookline before executing the vertical feature slice for **Geo-Discovery, Marketplace Search, Interactive Maps, and Provider Storefronts**. The application possesses a functional Clean Architecture ASP.NET Core 8 backend and a modern React 18 / TypeScript frontend with NodaTime, EF Core, Redis coordination, and multi-tenant isolation.

The goal of this development is to systematically execute Phase 1 through Phase 10 without discarding working functionality, adhering to the 13 non-negotiable engineering rules, and ensuring full end-to-end integration.

---

### 2. Current Architecture & Existing Subsystems

#### 2.1 Backend Solution Structure
- **`Bookline.Domain`**: 33 entities (`Tenant`, `Location`, `Service`, `ServiceCategory`, `Product`, `Booking`, `BookingHold`, `Review`, `Staff`, `WorkingHours`, etc.). Zero external dependencies.
- **`Bookline.Application`**: CQRS commands, queries, MediatR handlers, FluentValidation, `ISlotEngine`, `IGeocodingProvider`, `IProviderSearchService`.
- **`Bookline.Infrastructure`**: EF Core relational persistence (`BooklineDbContext`), `Npgsql.EntityFrameworkCore.PostgreSQL`, Redis distributed holds, `ProviderSearchService`, `SearchIntentService`, `GeocodingProvider`.
- **`Bookline.Api`**: Controllers for Discovery, Location, Bookings, Availability, Services, Products, Reviews, and Administration.
- **`Bookline.Worker`**: Outbox event processor and reminder background worker.

#### 2.2 Database & Multi-Tenancy Architecture
- Multi-tenancy is enforced via `TenantEntity` base class and EF Core global query filters (`_tenantContext.IsSystem || e.TenantId == _tenantContext.TenantId`).
- Public discovery queries explicitly call `.IgnoreQueryFilters()` and enforce publication invariants (`t.IsActive && t.IsPublished && t.ModerationStatus == ModerationStatus.Approved`).
- Database container runs `postgis/postgis:16-3.4-alpine` on port 5432.
- Indexing includes composite spatial indices: `builder.HasIndex(t => new { t.Latitude, t.Longitude })` and `builder.HasIndex(l => new { l.Latitude, l.Longitude })`.

#### 2.3 Frontend Architecture
- **Tech Stack**: React 18, TypeScript 5.6, Vite 5.4, React Router 6, TanStack Query 5, Tailwind CSS 3.4, Lucide React icons.
- **Design Tokens**: Standardized CSS variables in `src/styles/index.css` (Dark mode default `--bg-base: #090B10`, Surface `--surface-primary: #111620`, Accent `--accent-primary: #E8546A`, Status `--status-success: #34D399`, Typography: `DM Sans`).
- **Routing**: `src/app/router/index.tsx` covering customer routes (`/discover`, `/business/:slug`, `/book/:slug`), provider management routes, and administrator consoles.

---

### 3. Gap Analysis: What Works, Incomplete & Missing

| Domain Feature | Current Status | Findings & Implementation Plan |
| :--- | :---: | :--- |
| **Geocoding Abstraction** | Incomplete | `IGeocodingProvider` exists with `GeocodedLocation`, but lacks `PlaceSuggestion` model with autocomplete viewport bounds and `GeocodingStatus` state machine (`NotRequested`, `Pending`, `Verified`, `Failed`, `NeedsReview`). |
| **City/Area/Postcode Search** | Works | `SearchIntentService` extracts 6-digit postal codes (`\b\d{6}\b`), known areas (*Bodakdev*, *Satellite*, *Bandra*, etc.), and parent cities. Needs formal place suggestion endpoint. |
| **Provider Location Persistence** | Works | `Tenant` and `Location` persist verified coordinates, street address, city, state, postal code, phone, and timezone. |
| **PostGIS Spatial Search** | Incomplete | Database uses PostGIS image, but queries currently use composite B-Tree range bounds in SQL (`IQueryable` bounding envelope) + Haversine geodesic calculations. Native NetTopologySuite spatial types can be added without breaking existing queries. |
| **Discovery API** | Works | `GET /api/v1/discovery/providers` and `GET /api/v1/discovery/providers/{slug}` support text, category, city, radius, bounding box, rating, price, and sort options. |
| **Search Relevance & Ranking** | Works | 9-signal multi-factor economic scoring model in `ProviderSearchService.cs` combines text match (0.25), service match (0.15), distance (0.15), availability (0.10), rating (0.15), review volume (0.05), verification (0.05), reliability (0.05), completeness (0.05). |
| **Distance & Rating Sorting** | Works | Distance uses Haversine geodesic calculation. Top Rated sorting utilizes Bayesian damping $(R \cdot v + 20)/(v + 5)$ to prevent 1-review 5.0★ venues from beating established 120-review 4.9★ venues (Rule 8). |
| **Interactive Map & Markers** | Works | `InteractiveMap.tsx` provides interactive vector map with dragging, zoom, pin coordinate projection, cluster pills, and floating "Search this area" button. |
| **Provider Storefront & Catalogue**| Works | `ProviderStorefrontPage.tsx` renders business banner, gallery, operating hours, multi-branch cards, categorized service catalogue, products, staff, approved reviews, and 5-step booking flow. |
| **Booking Handoff & Concurrency** | Works | Redis distributed slot holds with TTL coordination prevent double booking before confirmation. |
| **Loading / Empty / Error States** | Works | Skeletons for loading, contextual empty states with reset filters action, and error alert banners with retry buttons. |

---

### 4. Baseline Build & Test Execution Record

Execution run verified on: **2026-10-12T00:23:05+05:30**

```text
======================================================================
TEST SUITE EXECUTION RECORD
======================================================================
Bookline.Domain.UnitTests:
  Passed: 21, Failed: 0, Skipped: 0 (Duration: 26 ms)
  - Slot duration, buffer calculation, appointment state invariants

Bookline.Infrastructure.UnitTests:
  Passed: 49, Failed: 0, Skipped: 0 (Duration: 10 s)
  - Intent NLP extraction, geocoding distances, repo persistence

Bookline.Application.UnitTests:
  Passed: 78, Failed: 0, Skipped: 0 (Duration: 10 s)
  - Section 131 Geo Search (9 scenarios: GPS granted, GPS denied, manual city,
    manual area, radius, bounding box, nearest sort, map movement, search this area)
  - Critical Concurrency (50-user Redis holds, 0 double bookings)
  - Section 132 Payment Webhook Idempotency
  - Section 133 Stock=1 Purchase Concurrency
  - Section 134 Cross-Tenant Access Attack Defense

Bookline.IntegrationTests:
  Passed: 4, Failed: 0, Skipped: 0 (Duration: 23 s)
  - Public availability HTTP endpoints, cross-tenant isolation

TOTAL: 152 PASSED, 0 FAILED, 0 SKIPPED (100% Pass Rate)

FRONTEND LINT & BUILD:
  npm run lint (tsc --noEmit): 0 errors
  npm run build (vite build): 1712 modules transformed, 0 errors
======================================================================
```

---

### 5. Architectural Constraints & Non-Negotiable Rules

1. **No Data Fabrication**: Never fabricate fake distances, static slot times (`"10:30 AM"`), or hardcoded prices. Real database state or explicit `"Check schedule"` / `"Custom pricing"` only.
2. **Database-Level Spatial Filtering**: Authoritative filtering must happen in SQL / PostgreSQL using bounding envelopes or PostGIS operations. Never load the entire table into application memory to calculate proximity.
3. **Public Endpoint Security**: Zero private customer or provider fields (e.g. passwords, Stripe secret keys, internal notes, customer phone numbers) exposed in public storefront and discovery DTOs.
4. **Resilient Map Fallbacks**: Map tile or network failures must never crash or disable the provider list view.
5. **Phase-by-Phase Discipline**: Develop one phase at a time, verify tests and builds, document outcomes, and pause at phase boundaries.

---

### 6. Phase Execution Roadmap

- **Phase 0**: Existing Repository Audit (Current - Complete).
- **Phase 1**: Domain Contracts and Public Data Model (Business vs. Location vs. Service distinction, public visibility rules, `GeocodingStatus`).
- **Phase 2**: Geolocation Domain and Provider Abstractions (`IGeocodingProvider`, `PlaceSuggestion`, address verification flow, fallback modes).
- **Phase 3**: PostGIS & Database Spatial Query Architecture (Spatial indexing, NetTopologySuite integration).
- **Phase 4**: Discovery API & Search Query Processing (`/api/v1/geo/places`, refined query processing, contract hardening).
- **Phase 5**: Multi-Signal Ranking & Search Relevance Hardening.
- **Phase 6**: Frontend Modular Feature Slicing (`features/geo`, `features/discovery`, `features/maps`, `features/storefront`).
- **Phase 7**: Interactive Map & Bidirectional Sync Hardening.
- **Phase 8**: Storefront, Service Catalogue & Booking Handoff.
- **Phase 9**: Comprehensive Automated Test Suite Expansion.
- **Phase 10**: Documentation & Production Release Package Verification.
