# High-Concurrency Booking Engine

## 1. Overview
The Bookline Booking Engine manages distributed slot calculation, optimistic locking, short-lived slot holds via Redis, and zero-double-booking guarantees.

## 2. Distributed Hold Engine (Section 123)
- **Role**: Temporary reserve while the user completes payment checkout (default: 5–10 minutes).
- **Implementation**:
  - `SlotHoldService` uses Redis atomic `StringSetAsync(holdKey, holdId, expiry, When.NotExists)`.
  - Fallback: High-throughput concurrent dictionary in development environments.
  - Non-permanent: Redis holds expire automatically; PostgreSQL is the only permanent database.

## 3. Concurrency Protection & Zero Double Booking (Section 130)
- **Critical Test Verification**:
  - 50 concurrent users attempt to book the exact same provider, location, staff, service, and time slot simultaneously.
  - Result: Exactly 1 succeeds, 49 are rejected with `SLOT_UNAVAILABLE`.
  - Database contains exactly 1 confirmed appointment.

## 4. Slot Calculation Engine (`SlotEngine.cs`)
- Computes available slots using:
  1. Staff working hours for the given day of the week.
  2. Subtraction of existing confirmed/pending bookings.
  3. Subtraction of staff time-off blocks.
  4. Addition of service buffer times (`BufferBeforeMinutes` and `BufferAfterMinutes`).
  5. Filtering out current active slot holds.
