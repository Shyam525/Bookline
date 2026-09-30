# Bookline Context & Engineering Standards

## Tech Stack
- **Runtime & SDK**: .NET 8 (`net8.0`)
- **OR/M**: Entity Framework Core 8 (`Microsoft.EntityFrameworkCore` 8.x, `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x)
- **Database**: PostgreSQL 16
- **Cache & Message Transport**: Redis 7
- **Background Jobs**: Hangfire 1.8+ (Redis storage backend)
- **CQRS / Dispatcher**: MediatR 12.x
- **Date/Time Handling**: NodaTime 3.x
- **Testing**: xUnit, FluentAssertions, Testcontainers 3.x, Microsoft.AspNetCore.Mvc.Testing
- **Containerization**: Docker & Docker Compose

## Architecture & Project Dependency Rules
```
[Bookline.Api]  \
                 --> [Bookline.Application] --> [Bookline.Domain] (ZERO Package References)
[Bookline.Worker] /             ^
                                |
                   [Bookline.Infrastructure]
```
1. `Bookline.Domain`: Pure domain entities, value objects, domain events, domain exceptions. **MUST HAVE ZERO `<PackageReference>` ENTRIES IN CSPROJ.**
2. `Bookline.Application`: Use cases, MediatR commands/queries, interfaces (`ITenantContext`, repositories), DTOs, FluentValidation validators. References `Domain`.
3. `Bookline.Infrastructure`: EF Core `DbContext`, persistence implementations, Redis caching, Hangfire jobs, external services. References `Application`.
4. `Bookline.Api`: ASP.NET Core controllers/endpoints, authentication, authorization policies, Serilog setup, OpenAPI/Swagger, health checks. References `Application` and `Infrastructure`.
5. `Bookline.Worker`: Background consumer service. References `Application` and `Infrastructure`.

## Multi-Tenancy Rules
- Every tenant-owned domain entity MUST inherit from `TenantEntity { public Guid TenantId { get; set; } }`.
- Scoped `ITenantContext` provides `TenantId` resolved from JWT `tenant_id` claim (or middleware).
- EF Core `DbContext` MUST apply global query filters on all `TenantEntity` classes: `.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)`.
- `SaveChangesInterceptor` MUST automatically stamp `TenantId` on entity insertion, and throw an exception if a modified/deleted entity's `TenantId` does not match the active `TenantContext`.
- `IgnoreQueryFilters()` is STRICTLY FORBIDDEN unless approved for explicit, commented system-admin/cross-tenant maintenance paths.

## Code Quality & Compiler Directives
- `<Nullable>enable</Nullable>` is enforced across all projects.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is enforced across all projects.
- Always run `dotnet build -warnaserror` and `dotnet test` after code modifications.
