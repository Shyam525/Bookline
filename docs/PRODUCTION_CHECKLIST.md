# Bookline — Production Readiness Checklist

This document defines the strict pre-flight operational gates required before onboarding real pilot businesses to Bookline.

## 1. Security & Compliance
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

## 2. Reliability & High-Availability
- [x] **PostgreSQL Overlap Protection**: Exclusion constraint on non-overlapping `tstzrange` intervals prevents double bookings at database level.
- [x] **Transactional Outbox Engine**: Appointments and notifications committed atomically; notifications process asynchronously with exponential retry backoff.
- [x] **Health Probes**:
  - Liveness probe at `/health` (`HTTP 200`).
  - Readiness probe at `/ready` checking PostgreSQL, Redis, and Outbox backlog.
- [x] **Failure Isolation**: Notification carrier failures (SMTP / SMS / WhatsApp) never roll back confirmed appointments.

## 3. Seed & Test Data Guard
- [x] **Production Seeding Block**: `MarketplaceDbSeeder` throws `InvalidOperationException` if `ASPNETCORE_ENVIRONMENT=Production`.
- [x] **Deterministic Pilot Profiles**: Seed data provides 5 deterministic Gujarat pilot businesses for testing in `Development`.
- [x] **Metric Integrity**: Fake dashboard metrics, hardcoded percentages, and mock appointments removed. Real data rendered with loading, error, and empty states.

## 4. Operational Runbooks
- [x] `docs/ENV_VARS.md`: Complete matrix of mandatory vs optional production variables.
- [x] `docs/DATABASE_MIGRATION.md`: Zero-downtime migration and rollback procedures.
- [x] `docs/BACKUP_AND_RESTORE.md`: Automated pg_dump and verified pg_restore drill.
- [x] `docs/MONITORING.md`: Metric alerts, backlog thresholds, and log correlation.
- [x] `docs/INCIDENT_RESPONSE.md`: Triage steps for database outages and carrier outages.
- [x] `docs/PILOT_ONBOARDING_GUIDE.md`: Step-by-step workflow for onboarding the 5 Gujarat pilot businesses.
