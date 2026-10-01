# REST API Specification

## Health & Diagnostics

- `GET /health`: Health check endpoint verifying DB and Redis status.
- `GET /swagger`: OpenAPI interactive documentation.

## Public Booking Endpoints

- `GET /api/v1/public/tenant/{slug}`: Fetch tenant metadata, available services, and staff.
- `GET /api/v1/public/availability`: Query available slots for a staff, service, date, timezone, and slug.
- `POST /api/v1/public/hold`: Acquire a 5-minute hold on a slot.
- `POST /api/v1/public/book`: Confirm an appointment using `staffId`, `serviceId`, `startUtc`, `holdId`, `customerName`, and `customerEmail`; `customerPhone` is optional. The customer is matched by email within the tenant or created with the booking.

## Authenticated Endpoints

- Business operations UI: `GET /admin.html`.
- `POST /api/v1/auth/login`: Authenticate and acquire JWT and refresh tokens.
- `POST /api/v1/auth/register-tenant`: Create a tenant and owner account.
- `GET /api/v1/staff`: List tenant staff members.
- `POST /api/v1/staff`: Create a staff member (Owner).
- `PUT /api/v1/staff/{id}/working-hours`: Set staff working hours (Owner).
- `GET /api/v1/services`: List tenant services.
- `POST /api/v1/services` and `PUT /api/v1/services/{id}`: Manage services (Owner).
- `GET /api/v1/customers`: List tenant customers.
- `GET /api/v1/bookings?fromUtc={start}&toUtc={end}&staffId={id}`: Read the tenant calendar for a UTC range of up to 31 days; `staffId` is optional. Requires the `ManageBookings` role.
- `POST /api/v1/bookings/{id}/confirm`: Confirm a pending appointment.
- `POST /api/v1/bookings/{id}/cancel`: Cancel a pending or confirmed tenant booking.
- `PUT /api/v1/bookings/{id}/reschedule`: Move a pending or confirmed booking using a valid 5-minute slot hold; body: `{ "startUtc": "...", "holdId": "..." }`.
