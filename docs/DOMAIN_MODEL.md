# Bookline Domain Model

The domain model represents pure enterprise scheduling rules with zero third-party framework dependencies.

## Core Entities
- **Tenant**: Multi-tenant business entity.
- **Service**: Offerings with duration, buffer time, and price.
- **Staff**: Team members providing services.
- **WorkingHours**: Weekly recurring availability intervals per staff member.
- **TimeOff**: Staff absences and holiday overrides.
- **Booking**: Appointment aggregate with status machine (`Pending`, `Confirmed`, `CheckedIn`, `Completed`, `Cancelled`, `NoShow`).
- **Customer**: Client directory record.
- **OutboxMessage**: Transactional event payload.
