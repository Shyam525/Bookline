# Bookline Developer Contribution Guidelines

## Engineering Workflow & Guidelines

When adding features or modifying code in Bookline, adhere strictly to these architectural guidelines:

1. **Phase Execution Pipeline**:
   `DESIGN → ARCHITECTURE → DATABASE → BACKEND → FRONTEND → TESTS → MANUAL VERIFICATION → BUG FIXING → ACCEPTANCE`

2. **Backend Domain Rules**:
   - Entities must inherit from `TenantEntity` if tenant-scoped.
   - Do NOT add EF Core or ASP.NET attributes to Domain entities.
   - Controllers must stay thin and delegate all logic to MediatR handlers.

3. **Frontend Rules**:
   - Availability algorithms and booking state rules MUST NOT live in React components.
   - Use reusable components from `components/ui`, `components/forms`, `components/data-display`, and `components/feedback`.

4. **Testing Rules**:
   - Run `dotnet test backend/tests/Bookline.Application.UnitTests/Bookline.Application.UnitTests.csproj -c Release` before committing code.

---
*Updated for Bookline 2026.*
