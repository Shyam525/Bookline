# Bookline Availability & Schedule Engine

## Realtime Slot Calculation Algorithm (`ISlotEngine`)

The availability engine calculates valid, non-overlapping appointment slots using NodaTime precision.

### Calculation Sequence
1. **Service Parameters**: Extract `DurationMinutes` and `BufferMinutes = BufferBefore + BufferAfter`.
2. **Staff Eligibility**: Filter active staff assigned to the requested service.
3. **Working Hours Window**: Load `WorkingHours` for the requested day of week.
4. **Interval Exclusion**:
   - Subtract active `Booking` intervals (`StartUtc` to `EndUtc`).
   - Subtract approved `TimeOff` intervals (`StartUtc` to `EndUtc`).
   - Subtract minimum notice requirement (`Duration.FromHours(1)`).
5. **Slot Step Alignment**: Generate 15-minute aligned starting times that fit the full service duration plus buffers within open windows.

---
*Updated for Phase 9 completion.*
