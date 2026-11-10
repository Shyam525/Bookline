# Bookline — Production & Pilot Readiness Checklist

This document defines the strict pre-flight operational gates required before onboarding real pilot businesses to Bookline.

## 1. Runtime & Framework Modernization (Phase 7.6)
- [x] **.NET 10 LTS Target**: All 9 backend projects target `net10.0` with `TreatWarningsAsErrors=true` and 0 errors / 0 warnings.
- [x] **Reproducible Development Infrastructure**: PostGIS 16, Redis 7, and Mailpit running in isolated Docker containers with automated health checks.
- [x] **Full Test Suite Execution**:
  - `Bookline.Domain.UnitTests`: 21 passed (100%).
  - `Bookline.Infrastructure.UnitTests`: 63 passed (100%).
  - `Bookline.Application.UnitTests`: 413 passed (100%).
  - `Bookline.IntegrationTests`: 46 passed against real PostgreSQL (100%).
  - Total backend tests: **543 passed (100%)**.
  - Frontend unit tests: **62 passed (100%)**.
  - Playwright end-to-end tests: **10 passed (100%)**.

## 2. Telemetry & Funnel Instrumentation (Phase 7.6)
- [x] **Provider Activity Telemetry**: `ProviderActivityEvent` records workspace opens, calendar views, updates, and completions. Deduplicated within 15-minute sliding windows; excludes bots, tests, and PlatformAdmin.
- [x] **Completion Metrics & Timestamps**: `Booking.CompletedAtUtc` (`DateTimeOffset?`) tracks physical appointment completion without backfilling historical data.
- [x] **Half-Open Reporting Windows**: Metrics queries calculate KPIs across $[weekStartUtc, weekEndUtc)$ to prevent edge duplicate counting.
- [x] **4-Step Storefront Funnel**: `StorefrontFunnelEvent` tracks `Visitor` $\to$ `AvailabilityViewed` $\to$ `HoldCreated` $\to$ `BookingConfirmed` with first-party pseudonymous session IDs.
- [x] **Quarantining Synthetic Data**: All seeded demo businesses marked `IsDemo = true` and quarantined from pilot KPIs.
- [x] **Real Pilot Prospect Pipeline**: Blank prospect tracker established in `docs/validation/real-pilot-prospect-tracker.md` restricted strictly to salons, barbershops, beauty studios, spas, and wellness.

## 3. Staging & Infrastructure Artifacts (Phase 7.6)
- [x] **Staging Docker Compose**: `docker-compose.staging.yml` configured with production-grade dependencies, container healthchecks, and internal networks.
- [x] **Staging Environment Template**: `.env.staging.example` with complete secret definitions and configuration variables.
- [x] **Nginx Staging Configuration**: `nginx/staging.conf` with SSL/TLS termination, HTTP/2, security headers, and proxy forwarding.
- [x] **Production Notification Strategy**: `docs/PRODUCTION_NOTIFICATION_STRATEGY.md` detailing Meta WhatsApp Cloud API, Indian TRAI DLT compliance (PE, Header, Content Template), transactional SMTP, and outbox failure isolation.

## 4. Security & Compliance
- [x] **JWT Secret Strength**: `JwtSettings:Secret` configured with >= 32 cryptographically secure bytes; fallback demo secrets rejected in Production (`ASPNETCORE_ENVIRONMENT=Production`).
- [x] **Tenant Isolation**: EF Core global query filters (`TenantEntity`) enforced across all tenant entities. Cross-tenant queries return 404.
- [x] **Role-Based Access Control**:
  - `RequirePlatformAdmin` policy enforced on all `/api/v1/admin/*` routes.
  - Providers and ordinary customers are strictly blocked from platform governance.
- [x] **Public Endpoint Rate Limiting**:
  - Auth limiter: 15 req/min.
  - Registration limiter: 10 req/min.
  - Hold limiter: 30 req/min.
  - Webhooks limiter: 300 req/min with queueing.
- [x] **Secrets & Credential Redaction**:
  - Audit logs sanitize tokens, API keys, and passwords (`[REDACTED]`).
  - Health checks never leak connection strings or server exceptions.
- [x] **Customer Privacy**: Provider internal notes are never exposed on customer storefront or guest booking endpoints.

## 5. Reliability & High-Availability
- [x] **PostgreSQL Overlap Protection**: Exclusion constraint on non-overlapping `tstzrange` intervals prevents double bookings at database level.
- [x] **Transactional Outbox Engine**: Appointments and notifications committed atomically; notifications process asynchronously with exponential retry backoff.
- [x] **Health Probes**:
  - Liveness probe at `/health` (`HTTP 200`).
  - Readiness probe at `/ready` checking PostgreSQL, Redis, and Outbox backlog.
- [x] **Failure Isolation**: Notification carrier failures (SMTP / SMS / WhatsApp) never roll back confirmed appointments.

## 6. Operational Runbooks
- [x] `docs/ENV_VARS.md`: Complete matrix of mandatory vs optional production variables.
- [x] `docs/DATABASE_MIGRATION.md`: Zero-downtime migration and rollback procedures.
- [x] `docs/BACKUP_AND_RESTORE.md`: Automated pg_dump and verified pg_restore drill.
- [x] `docs/MONITORING.md`: Metric alerts, backlog thresholds, and log correlation.
- [x] `docs/INCIDENT_RESPONSE.md`: Triage steps for database outages and carrier outages.
- [x] `docs/PILOT_ONBOARDING_GUIDE.md`: Step-by-step workflow for onboarding the 5 Gujarat pilot businesses.
- [x] `docs/PRODUCTION_NOTIFICATION_STRATEGY.md`: Transport routing, Meta WhatsApp templates, and TRAI DLT SMS guide.
- [x] `docs/validation/real-pilot-prospect-tracker.md`: Blank Gujarat pilot prospect acquisition pipeline.
