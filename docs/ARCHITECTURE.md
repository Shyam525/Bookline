# Bookline Architecture Overview

## Modular Clean Architecture

Bookline follows a strict **Modular Clean Architecture** pattern designed for enterprise scalability, testability, and strict separation of concerns.

```text
c:\z-projects\Bookline\src\
  ├── Bookline.Domain          (Core domain models, entities, value objects, domain events)
  ├── Bookline.Application     (Use cases, CQRS queries/commands, interfaces, slot engine)
  ├── Bookline.Infrastructure  (Persistence, EF Core, Redis slot holds, Outbox, jobs)
  ├── Bookline.Api             (ASP.NET Core Web API, controllers, middleware, wwwroot UI)
  └── Bookline.Worker          (Background Outbox and reminder worker service)
```

## Architectural Principles

1. **Authoritative Persistence (PostgreSQL)**:
   - PostgreSQL is the sole canonical source of truth for organizations, locations, staff, services, working hours, and appointments.
   - Exclusion constraints (`EXCLUDE USING gist`) prevent double booking at the database level.

2. **Temporary Slot Coordination (Redis)**:
   - Redis is used strictly for short-lived, transient slot holds (e.g. 5-minute hold during checkout).
   - Redis holds automatically expire without degrading persistent database records.

3. **Domain Time Correctness (NodaTime)**:
   - All appointment timestamps are stored in UTC (`StartUtc`, `EndUtc`) using NodaTime `Instant`.
   - Local calendar representation relies on `LocalDate` and location timezones (`America/New_York`, `Asia/Kolkata`).

4. **Reliable Side Effects (Transactional Outbox)**:
   - Appointments and outbox messages are saved within a single atomic database transaction.
   - Background workers publish notifications asynchronously with retries and dead-letter handling.
