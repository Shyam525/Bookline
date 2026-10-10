# Comprehensive Test Architecture

## 1. Test Architecture Overview (Section 129)
The Bookline test suite spans three distinct tiers ensuring end-to-end reliability, mathematical concurrency correctness, and multi-tenant isolation.

```text
┌────────────────────────────────────────────────────────┐
│                   E2E Tests (Playwright)               │
│ Discovery, Map Sync, Storefront, Booking, Reschedule   │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│              Integration Tests (xUnit + WebApp)        │
│ API Contracts, DB Interceptors, Multi-Tenant, Outbox   │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                 Unit Tests (xUnit + EF InMemory)       │
│ Domain Invariants, Slot Engine, Holds, Geo, Security   │
└────────────────────────────────────────────────────────┘
```

## 2. Critical Specification Tests

### A. Critical Concurrency Test (Section 130)
- **File**: `CriticalConcurrencyTests.cs`
- **Specification**: 50 concurrent users attempt to book the exact same slot for the same provider, location, and staff simultaneously.
- **Verification**: Exactly 1 succeeds, 49 rejected with `SLOT_UNAVAILABLE`. Exactly 1 confirmed appointment exists in the database.

### B. Geo Search Specification Tests (Section 131)
- **File**: `GeoSearchSpecificationTests.cs`
- **Coverage**: All 9 scenarios:
  1. Current location granted
  2. Current location denied
  3. Manual city
  4. Manual area
  5. Radius
  6. Bounding box
  7. Nearest
  8. Map movement
  9. Search this area

### C. Payment Webhook Replay Test (Section 132)
- **File**: `SystemVerificationTests.cs`
- **Specification**: Webhook processed; replay of identical webhook acknowledged without duplicate payment or order transitions.

### D. Inventory Concurrency Test (Section 133)
- **File**: `SystemVerificationTests.cs`
- **Specification**: Product with Stock = 1; two customers purchase concurrently; exactly 1 succeeds, inventory never becomes negative (-1).

### E. Tenant Attack Test (Section 134)
- **File**: `SystemVerificationTests.cs`
- **Specification**: Provider A maliciously queries or modifies Provider B's appointments, customers, orders, staff, or analytics.
- **Verification**: All cross-tenant queries return null / empty, and modifications throw `InvalidOperationException` ("Cross-tenant modification or deletion attempted.").

## 3. Running the Test Suite
```bash
powershell -Command "Get-ChildItem -Path 'backend' -Recurse -Filter '*.dll' | Unblock-File; dotnet test backend/Bookline.slnx"
```
