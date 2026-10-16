# PART VII — SECTION 85: POSTGRESQL QUERY AND INDEX ENGINEERING

## Executive Architectural Summary

This document formalizes the production query and indexing strategy for **Bookline**, based on measured workload access patterns, query plans, concurrency requirements, and multi-tenant isolation guarantees.

Indexes in Bookline are designed from actual runtime query shapes rather than speculative or blanket column indexing. All added indexes are bounded, support deterministic sorting, avoid duplicate B-tree overhead, and protect transaction safety.

---

## 1. Workload Query Profiles and Index Mapping

### 1.1 Appointments and Availability Engine (`Bookings`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index / Constraint |
| :--- | :--- | :--- | :--- |
| **Provider Calendar / Agenda** | `WHERE TenantId = @t AND LocationId = @loc AND StartUtc >= @from AND StartUtc <= @to ORDER BY StartUtc` | Index Scan bounded by `(TenantId, LocationId, StartUtc)` | `IX_Bookings_TenantId_LocationId_StartUtc_EndUtc` |
| **Staff Availability Conflicts** | `WHERE StaffId = @staff AND Status != 'Cancelled' AND StartUtc < @to AND EndUtc > @from` | Index Seek directly on staff timeline without tenant prefix scan | `IX_Bookings_StaffId_StartUtc_EndUtc_Status` |
| **Resource Exclusive Occupancy** | `WHERE TenantId = @t AND ResourceId = @res AND Status IN ('Pending', 'Confirmed', 'CheckedIn') AND StartUtc < @end AND EndUtc > @start` | Index Seek on resource timeline for room/equipment conflict check | `IX_Bookings_TenantId_ResourceId_StartUtc_EndUtc` |
| **Customer Appointment History** | `WHERE CustomerId = @c [AND Status = @status] ORDER BY StartUtc DESC` | Index Scan on customer bookings with pre-sorted chronological B-tree | `IX_Bookings_CustomerId_StartUtc_Status` |
| **Database Concurrency Invariant (Staff)** | Overlapping interval rejection for identical staff | GiST Exclusion constraint enforcing physical non-overlap on `tstzrange` | `no_overlap EXCLUDE USING gist (StaffId WITH =, tstzrange(StartUtc, EndUtc) WITH &&) WHERE (Status IN ('Pending','Confirmed'))` |
| **Database Concurrency Invariant (Resource)** | Overlapping interval rejection for identical physical resource | GiST Exclusion constraint enforcing physical non-overlap on `tstzrange` | `no_resource_overlap EXCLUDE USING gist (ResourceId WITH =, tstzrange(StartUtc, EndUtc) WITH &&) WHERE (ResourceId IS NOT NULL AND Status IN ('Pending','Confirmed'))` |

---

### 1.2 Commerce and Orders (`Orders`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index |
| :--- | :--- | :--- | :--- |
| **Provider Orders by State** | `WHERE TenantId = @t AND Status = @status ORDER BY CreatedAtUtc DESC, Id` | Forward Index Scan avoiding in-memory sort or filesort | `IX_Orders_TenantId_Status_CreatedAtUtc` |
| **Provider Orders Feed** | `WHERE TenantId = @t ORDER BY CreatedAtUtc DESC, Id` | Forward Index Scan for overall provider commerce dashboard | `IX_Orders_TenantId_CreatedAtUtc` |
| **Customer Order History** | `WHERE CustomerId = @c ORDER BY CreatedAtUtc DESC, Id` | Forward Index Scan on customer commerce timeline | `IX_Orders_CustomerId_CreatedAtUtc` |

---

### 1.3 Customer and Provider Reviews (`Reviews`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index |
| :--- | :--- | :--- | :--- |
| **Provider Public Reviews (Newest)** | `WHERE TenantId = @t AND ModerationStatus = 'Approved' ORDER BY CreatedAtUtc DESC, Id` | Direct Index Scan on approved reviews | `IX_Reviews_TenantId_ModerationStatus_CreatedAtUtc` |
| **Provider Public Reviews (Top Rated)** | `WHERE TenantId = @t AND ModerationStatus = 'Approved' ORDER BY Rating DESC, CreatedAtUtc DESC, Id` | Multi-key Index Scan on rating and chronology | `IX_Reviews_TenantId_ModerationStatus_Rating_CreatedAtUtc` |
| **Customer My Reviews** | `WHERE CustomerId = @c ORDER BY CreatedAtUtc DESC, Id` | Chronological customer review retrieval | `IX_Reviews_CustomerId_CreatedAtUtc` |

---

### 1.4 Financial Transactions & Webhooks (`Payments`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index |
| :--- | :--- | :--- | :--- |
| **Stripe Payment Intent Webhook** | `WHERE StripePaymentIntentId = @pi` | Unique/Selective Index Seek on external Stripe identifier | `IX_Payments_StripePaymentIntentId` |
| **Stripe Checkout Session Webhook** | `WHERE StripeCheckoutSessionId = @cs` | Selective Index Seek on checkout session identifier | `IX_Payments_StripeCheckoutSessionId` |
| **Provider Financial Ledger** | `WHERE TenantId = @t [AND Status = @status] ORDER BY CreatedAtUtc DESC` | Index Scan on provider financial transactions | `IX_Payments_TenantId_Status_CreatedAtUtc` |
| **Booking Payment Lookup** | `WHERE BookingId = @bookingId` | Direct Foreign Key Index Seek for appointment payment settlement | `IX_Payments_BookingId` |
| **Customer Payment History** | `WHERE CustomerId = @c ORDER BY CreatedAtUtc DESC` | Chronological customer transaction history | `IX_Payments_CustomerId_CreatedAtUtc` |

---

### 1.5 Marketplace Discovery & Geospatial Search (`Tenants`, `Locations`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index |
| :--- | :--- | :--- | :--- |
| **Public Active Storefronts** | `WHERE IsActive = true AND IsPublished = true AND ModerationStatus = 'Approved' AND City = @city [AND AverageRating >= @r]` | Composite covering index for public discovery | `IX_Tenants_Discovery` (`IsActive`, `IsPublished`, `ModerationStatus`, `City`, `AverageRating`) |
| **Geospatial Proximity Search** | `WHERE ST_DWithin(geom, ST_MakePoint(lon, lat), radius)` or bounding box | Spatial B-tree / GiST on geographical coordinates | `IX_Tenants_Latitude_Longitude`, `IX_Locations_Latitude_Longitude` |
| **Provider Location Branches** | `WHERE TenantId IN (...) AND IsActive = true AND IsArchived = false` | Index Seek on active branches | `IX_Locations_TenantId_IsActive_IsArchived` |

---

### 1.6 High-Throughput Worker Queues (`OutboxMessages`, `NotificationLogs`)

| Access Path | Query Predicates & Ordering | Intended Access Method | Supporting Index |
| :--- | :--- | :--- | :--- |
| **Outbox Polling Worker** | `WHERE Status = 'Pending' AND RetryCount < 5 ORDER BY CreatedAtUtc LIMIT 20` | High-frequency $O(1)$ polling queue index | `IX_OutboxMessages_Status_RetryCount_CreatedAtUtc` |
| **Customer Notification Center** | `WHERE RecipientEmail = @email [AND IsRead = false] ORDER BY CreatedAtUtc DESC` | Instant user feed index with read/unread filtering | `IX_NotificationLogs_RecipientEmail_IsRead_CreatedAtUtc` |

---

## 2. Time-Interval Conflicts & PostgreSQL GiST Exclusion Constraints

### 2.1 The Limits of Standard B-Tree Indexes
A standard B-tree index on `(StaffId, StartUtc, EndUtc)` can only accelerate range queries like:
```sql
WHERE "StaffId" = @staff AND "StartUtc" < @newEnd AND "EndUtc" > @newStart
```
However, standard B-tree indexes **cannot prevent race conditions** where two concurrent transactions query at the exact same millisecond, see zero rows returned, and simultaneously insert overlapping appointments.

### 2.2 PostgreSQL GiST Exclusion Semantics
Bookline enforces strict database-level concurrency protection using PostgreSQL `EXCLUDE USING gist`:
```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- Staff Overlap Invariant
ALTER TABLE "Bookings" ADD CONSTRAINT no_overlap
  EXCLUDE USING gist (
    "StaffId" WITH =,
    tstzrange("StartUtc", "EndUtc") WITH &&
  )
  WHERE ("Status" IN ('Pending','Confirmed'));

-- Physical Resource Overlap Invariant
ALTER TABLE "Bookings" ADD CONSTRAINT no_resource_overlap
  EXCLUDE USING gist (
    "ResourceId" WITH =,
    tstzrange("StartUtc", "EndUtc") WITH &&
  )
  WHERE ("ResourceId" IS NOT NULL AND "Status" IN ('Pending','Confirmed'));
```

**Semantics Guaranteed**:
1. `tstzrange("StartUtc", "EndUtc") WITH &&`: Enforces that any half-open intervals $[Start, End)$ cannot overlap if `StaffId` or `ResourceId` matches.
2. `WHERE ("Status" IN ('Pending','Confirmed'))`: Excludes cancelled, completed, or no-show appointments, allowing cancelled slots to be immediately re-booked.
3. Violation Error Code `23P01`: Triggers standard RFC 7807 `409 Conflict` (`SLOT_UNAVAILABLE`) with suggested alternative slots computed by `SlotEngine`.

---

## 3. Migration Safety and Lock Considerations

When deploying indexes on active production databases:
1. **Locking Risk**: Standard `CREATE INDEX` acquires a `SHARE` lock on the target table, blocking all concurrent `INSERT`, `UPDATE`, and `DELETE` transactions until index creation finishes.
2. **Concurrent Indexing Strategy**: For high-volume production deployments, execute with `CONCURRENTLY`:
   ```sql
   CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Bookings_StaffId_StartUtc_EndUtc_Status"
   ON "Bookings" ("StaffId", "StartUtc", "EndUtc", "Status");
   ```
3. **Deterministic Cleanliness**: Migration `20261016183846_Section85QueryDrivenIndexes` removes superseded redundant indexes before creating multi-column covering indexes, preventing index bloat and redundant write amplification.
