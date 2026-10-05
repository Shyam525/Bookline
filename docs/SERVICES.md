# Bookline Service Catalog Documentation

## Service Specifications & Categories

The Service Catalog enables businesses to structure service offerings with granular controls:

### Features & Controls
- **Categories**: Logical grouping (`ServiceCategory`) with sort order.
- **Duration & Buffers**:
  - `DurationMinutes`: Pure service time.
  - `BufferBeforeMinutes`: Setup time before appointment.
  - `BufferAfterMinutes`: Cleanup time after appointment.
  - `TotalDurationMinutes`: Total block time reserved on calendar.
- **Pricing & Currency**: Base price and currency code.
- **Online Booking Toggle**: `IsOnlineBookingEnabled` (public vs internal only).
- **Color Tagging**: `ColorHex` for calendar visual differentiation.
- **Duplicate Action**: One-click cloning of service configurations.

---
*Updated for Phase 7 completion.*
