# Bookline — Elite Production-Grade Appointment Platform

Bookline is a serious SaaS appointment scheduling and business operations platform engineered for salons, clinics, spas, barbershops, wellness centers, and appointment-based practices.

---

## Completed Architecture Phases (Phases 0 — 10)

- [x] **Phase 0 — Foundation**: ASP.NET Core 8 Clean Architecture solution, React 18 frontend, PostgreSQL, Redis, Mailpit, Docker Compose.
- [x] **Phase 1 — Design System**: Dark luxury design system tokens (`#0A0C13`, `#111520`, `#E8546A`, Playfair Display / DM Sans) & component primitives.
- [x] **Phase 2 — Application Shell**: Top bar, grouped sidebar navigation, command palette (`Ctrl+K`), and notification center.
- [x] **Phase 3 — Authentication**: JWT auth, refresh token rotation, user registration/login, AuthProvider.
- [x] **Phase 4 — Multi-Tenancy**: Organization memberships, invitations, server-side EF query filters, and tenant isolation tests.
- [x] **Phase 5 — Organization Onboarding**: 10-step wizard engine (`OnboardingStatusDto`, step handlers, UI wizard).
- [x] **Phase 6 — Location Management**: Physical branch CRUD, timezone/currency settings, soft archival, and location UI.
- [x] **Phase 7 — Service Catalog**: Categories, duration, buffer intervals, pricing, color coding, duplicate service action, and catalog UI.
- [x] **Phase 8 — Staff Management**: Team member profiles, service mapping junction, working hours overlap validation, and team UI.
- [x] **Phase 9 — Availability & Schedule Engine**: Realtime `ISlotEngine` slot computation, NodaTime intervals, working windows, time-off exceptions, and schedule UI.
- [x] **Phase 10 — Customer Directory & CRM**: Client profiles, lifetime booking counts, lifetime spend tracking, notes drawer, paged search, and CRM UI.

---

## Tech Stack
- **Backend**: ASP.NET Core 8, C#, Entity Framework Core, PostgreSQL, Redis, NodaTime, MediatR CQRS, FluentValidation, xUnit.
- **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, Lucide Icons.
