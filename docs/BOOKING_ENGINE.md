# Bookline Booking & Availability Engine

## Slot Computation Pipeline

The core availability logic is executed by `SlotEngine` (`ISlotEngine`) in `Bookline.Application`.

```text
Working Hours + Location TimeZone
        ↓
    Day Windows (e.g. 09:00 - 17:00)
        ↓
  Subtract Staff Time-Off & Breaks
        ↓
  Subtract Existing Bookings & Holds
        ↓
  Evaluate Service Duration + Buffers
        ↓
   Generate Valid Available Slots
```

## Buffer-Aware Scheduling

Each service specifies:
- **DurationMinutes**: The active appointment duration (e.g. 45 min).
- **BufferMinutes**: Preparation/cleanup buffer time required after the appointment (e.g. 15 min).

The Slot Engine evaluates total occupied time:
$$\text{Occupied Interval} = \text{Duration} + \text{Buffer}$$

## Concurrency & Double Booking Prevention

Bookline enforces concurrency control at two independent layers:
1. **Redis Slot Hold Service**: Atomic hold acquisition with TTL during slot selection.
2. **PostgreSQL GiST Exclusion Constraint**:
```sql
ALTER TABLE "Bookings" ADD CONSTRAINT no_overlap
EXCLUDE USING gist (
    "StaffId" WITH =,
    tstzrange("StartUtc", "EndUtc") WITH &&
)
WHERE ("Status" IN ('Pending','Confirmed'));
```

If two concurrent requests attempt to confirm the same slot, exactly one transaction succeeds while the second fails with a `SLOT_UNAVAILABLE` domain exception.
