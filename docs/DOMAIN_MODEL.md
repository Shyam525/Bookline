# Bookline Domain Model Guide

## Authoritative Entities

Bookline's domain model represents multi-tenant appointment scheduling capabilities:

### Core Domain Entities
- **Tenant** (`Id`, `Name`, `Slug`, `Status`): Root organization tenant.
- **Location** (`Id`, `TenantId`, `Name`, `Address`, `Phone`, `Timezone`, `Currency`): Physical location branch.
- **ServiceCategory** (`Id`, `TenantId`, `Name`, `Description`, `SortOrder`): Categorization for services.
- **Service** (`Id`, `TenantId`, `CategoryId`, `Name`, `DurationMinutes`, `BufferBeforeMinutes`, `BufferAfterMinutes`, `Price`): Service offerings.
- **Staff** (`Id`, `TenantId`, `Name`, `Email`, `Phone`, `Title`, `Bio`, `TimeZoneId`): Staff team members.
- **StaffService** (`StaffId`, `ServiceId`): Junction entity mapping staff capabilities.
- **WorkingHours** (`Id`, `StaffId`, `DayOfWeek`, `StartTime`, `EndTime`): Weekly working hours per staff.
- **TimeOff** (`Id`, `StaffId`, `StartUtc`, `EndUtc`, `Reason`): Staff vacation and leave intervals.
- **Customer** (`Id`, `TenantId`, `FirstName`, `LastName`, `Email`, `Phone`, `Notes`, `TotalSpentAmount`): Client CRM directory.
- **Booking** (`Id`, `TenantId`, `StaffId`, `ServiceId`, `CustomerId`, `StartUtc`, `EndUtc`, `Status`): Authoritative booking appointments.

---
*Updated for Phase 6 - Phase 10 completion.*
