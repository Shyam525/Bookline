# Bookline Architecture Guide

## Clean Architecture Principles

Bookline is built following strict Clean Architecture and Modular Domain principles.

```
Bookline.Api (Controllers, Middleware, Authorization)
   └── Bookline.Application (Use Cases, CQRS Commands/Queries, DTOs, Validation)
          └── Bookline.Domain (Entities, Value Objects, Domain Exceptions)
   └── Bookline.Infrastructure (Persistence, EF Core, Interceptors, Outbox, Redis)
```

### Dependency Rules
1. **Domain Layer**: Zero dependencies on Infrastructure, ASP.NET Core, or EF Core. Contains authoritative domain entities (`Location`, `Service`, `Staff`, `Customer`, `Booking`).
2. **Application Layer**: Depends only on Domain. Implements MediatR CQRS requests, handlers, and validation rules.
3. **Infrastructure Layer**: Implements Application abstractions (`IApplicationDbContext`, `ITenantContext`, `ISlotEngine`). Handles EF Core database context, global query filters, and save interceptors.
4. **API Layer**: Coordinates request processing, JWT authentication middleware, and REST response formatting.

---
*Updated for Phase 6 - Phase 10 completion.*
