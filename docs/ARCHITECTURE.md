# Bookline Architecture & Technical Blueprint

## 1. Executive Overview
Bookline is a multi-tenant, high-concurrency marketplace platform for appointment-based services and physical commerce. Built with ASP.NET Core 8 (.NET 8) backend and a React + Vite TypeScript frontend, the system guarantees strong multi-tenant isolation, millisecond-level spatial search, idempotent financial processing, and zero-double-booking concurrency guarantees.

## 2. Clean Architecture Layering

```text
┌─────────────────────────────────────────────────────────────────┐
│                    Bookline.Api (Presentation)                  │
│  Controllers, Middlewares (Correlation, Security, Auth, Global) │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                 Bookline.Application (Use Cases)                │
│  Commands, Queries, Handlers, DTOs, FluentValidation, Ports     │
└───────────────┬─────────────────────────────────┬───────────────┘
                │                                 │
┌───────────────▼────────────────┐ ┌──────────────▼───────────────┐
│   Bookline.Domain (Entities)   │ │ Bookline.Infrastructure (Adapters) │
│ Entities, Value Objects, Enums │ │ EF Core, Redis, Hangfire, PostGIS  │
│ Business Logic, Invariants     │ │ Outbox Worker, Geocoding, Mailpit │
└────────────────────────────────┘ └──────────────────────────────┘
```

### Dependency Inversion Rules
1. **Domain Layer**: Pure business models with zero dependencies on frameworks, databases, or third-party libraries. Contains core aggregates (`Tenant`, `Location`, `Staff`, `Service`, `Product`, `Booking`, `Order`, `Payment`, `Review`).
2. **Application Layer**: Contains use cases and application boundaries (`IApplicationDbContext`, `ISlotEngine`, `ISlotHoldService`, `IIdempotencyService`, `IPaymentProvider`, `IPayoutProvider`).
3. **Infrastructure Layer**: Implements all persistence, caching, and external service contracts via PostgreSQL, PostGIS, Redis, Serilog, and Hangfire.
4. **API Layer**: Serves as the HTTP host, configuring dependency injection, JWT bearer authorization, rate limiting, and observability endpoints.

## 3. High-Concurrency & Distributed Coordination
- **Permanent Booking Database**: PostgreSQL 16 is the authoritative, permanent transactional store for all tenant entities and financial records.
- **Redis Coordination Role (Section 123)**:
  - Redis is used strictly for short-lived distributed slot holds (`StringSetAsync` with `When.NotExists`), distributed coordination, query caching, and rate limiting.
  - Never used as the permanent booking database.
- **Zero Double-Booking Guarantee (Section 130)**:
  - 50 concurrent users requesting the same slot at the same time: exactly 1 succeeds, 49 are rejected with `SLOT_UNAVAILABLE`, and exactly 1 booking is created in PostgreSQL.

## 4. Multi-Tenant Isolation
- **Global Query Filters**: Automatically applied to all `TenantEntity` classes:
  ```csharp
  builder.Entity<TenantEntity>().HasQueryFilter(e => _tenantContext.IsSystem || e.TenantId == _tenantContext.TenantId);
  ```
- **Tenant Save Interceptor**: Intercepts EF Core `SaveChanges` calls to reject cross-tenant writes or updates (`"Cross-tenant modification or deletion attempted."`).

## 5. Observability & Telemetry (Section 124)
- `/health`: Liveness probe.
- `/ready`: Readiness probe verifying PostgreSQL, Redis, and Outbox queue.
- `CorrelationIdMiddleware`: Propagates `X-Correlation-ID` header and enriches Serilog `LogContext` with `TraceId` and `CorrelationId`.
