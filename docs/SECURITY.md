# Security Architecture & Policies

## Authentication & Authorization

- **JWT Authentication**: Secure Bearer token authentication with configurable Secret Key, Issuer, Audience, and Expiration.
- **Role-Based Access Control (RBAC)**:
  - `Owner`: Full organizational control.
  - `Admin` / `Manager`: Location and staff management.
  - `Staff`: Individual schedule and appointment management.
  - `Receptionist`: Desk operational booking, check-in, and rescheduling.
  - `Viewer`: Read-only access.

## Data Protection

- **Password Security**: Passwords hashed using PBKDF2 with SHA-256 and unique salt.
- **Tenant Isolation**: Server-enforced EF Core global query filters prevent cross-tenant data exposure.
- **Input Validation**: FluentValidation validators sanitize and validate all request payloads.
