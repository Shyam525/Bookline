# PART IX — SECTION 87: NODATIME AND CLOCK CORRECTNESS

## 1. Architectural Principles

Business scheduling, slot calculation, hold expiry, cancellation policy enforcement, reminder dispatching, and financial reporting must **never** rely on machine-local clocks or ungrounded `DateTime.Now` / `DateTime.UtcNow`.

Bookline adopts **NodaTime** as the authoritative temporal foundation across the backend:

| Temporal Concept | NodaTime Type | Usage in Bookline |
|---|---|---|
| **Canonical Point in Time** | `Instant` | Appointment start/end, hold creation/expiry, audit log timestamps, outbox events |
| **Calendar Date** | `LocalDate` | Business operating schedule day, public booking availability queries |
| **Wall-Clock Time** | `LocalTime` | Staff working hours window (`09:00` - `17:00`), service buffers |
| **Timezone-Aware Instant** | `ZonedDateTime` | Customer-facing slot display, reminder email and SMS local times |
| **Timezone Reference** | `DateTimeZone` | Provider location timezone (`tenant.TimeZoneId`, `staff.TimeZoneId`) from Tzdb |
| **Time Interval** | `Duration` | Service duration, buffer duration, hold duration, minimum notice |
| **Occupancy Window** | `Interval` | Working windows, busy intervals (bookings + buffer, time-off) |

---

## 2. Server-Authoritative Clock (`IClock`)

Current-time decisions are decoupled from the host operating system through the `NodaTime.IClock` abstraction:
- In production, registered as a singleton with `NodaTime.SystemClock.Instance`.
- In test suites, injected using deterministic `TestClock` instances that can advance time predictably (`clock.Advance(duration)`).

### Authoritative Policies Enforced Against `IClock`:
1. **Hold Expiry**: Holds acquired via `ISlotHoldService` expire strictly when `IClock.GetCurrentInstant() > hold.ExpiresAtInstant`.
2. **Minimum Advance Booking Notice**: Bookings must satisfy:
   $$\text{StartInstant} \ge \text{IClock.GetCurrentInstant()} + \text{Duration.FromHours}(\text{MinimumNoticeHours})$$
3. **Booking Horizon**: Bookings cannot exceed:
   $$\text{StartInstant} \le \text{IClock.GetCurrentInstant()} + \text{Duration.FromDays}(\text{BookingHorizonDays})$$
4. **Cancellation Cutoff**: Customer-initiated cancellations are rejected if:
   $$\text{StartInstant} < \text{IClock.GetCurrentInstant()} + \text{Duration.FromHours}(\text{MinimumNoticeHours})$$

---

## 3. Location Timezone Reporting Boundaries

When computing business analytics (revenue, bookings, staff utilization) over a date range:
1. Date-only filters (`StartDate`, `EndDate`) are parsed as `LocalDate`.
2. Boundaries are projected into the provider's `tenant.TimeZoneId`:
   - Start Boundary: `startLocalDate.AtStartOfDayInZone(zone).ToInstant()`
   - End Boundary: `endLocalDate.PlusDays(1).AtStartOfDayInZone(zone).ToInstant()`
3. In daily chart aggregations, payments and bookings are mapped to local calendar days using:
   `Instant.FromDateTimeOffset(b.StartUtc).InZone(zone).Date`
   This prevents UTC offset leakage (e.g. 11:30 PM New York bookings shifting to the next calendar date in UTC).

---

## 4. Timezone Validation Standard

All timezone identifiers supplied through public endpoints, onboarding requests, or staff updates are validated against `NodaTime.DateTimeZoneProviders.Tzdb.GetZoneOrNull(id)`.
- If invalid: Rejection with `INVALID_TIMEZONE: '{id}' is not a valid IANA timezone identifier.`
- If missing: Graceful fallback to the provider's registered location timezone, with terminal fallback to `UTC`.
