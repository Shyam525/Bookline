# Bookline Automated Testing Strategy

## Test Coverage & Execution Guidelines

Bookline maintains high code quality through xUnit unit tests and isolated in-memory EF Core database test suites.

### Current Test Statistics
- **Total Unit Tests**: **51 active tests**
- **Test Projects**: `Bookline.Application.UnitTests`
- **Execution Command**:
  ```powershell
  dotnet test backend/tests/Bookline.Application.UnitTests/Bookline.Application.UnitTests.csproj -c Release
  ```

### Key Test Suites
- `TenantIsolationTests.cs`: Global query filter verification across multi-tenant boundaries.
- `LocationCommandHandlerTests.cs`: Location CRUD and soft archival tests.
- `ServiceCommandHandlerTests.cs`: Service duration calculation, buffer validation, and duplication tests.
- `StaffCommandHandlerTests.cs`: Staff profile CRUD, service mapping, and working hours overlap validator tests.
- `AvailabilityQueryHandlerTests.cs`: Realtime slot computation and time-off exception tests.
- `CustomerCommandHandlerTests.cs`: Customer CRM directory search, lifetime value, and soft deletion tests.

---
*Updated for Phase 10 test verification.*
