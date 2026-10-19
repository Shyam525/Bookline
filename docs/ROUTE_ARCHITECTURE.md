# Bookline — Route Architecture & Domain Boundary Specification

## 1. Overview
Bookline enforces strict, clean separation across four primary application domains and execution surfaces:
1. **Public Marketplace**: Discovery, Geolocation Search, Provider Storefront (`/business/:slug`), Booking Flow (`/book/:slug`), and Cart Checkout.
2. **Authentication & Recovery**: Secured credential validation, registration, and reset flows.
3. **Customer Account Domain**: Authenticated personal history, appointments, order tracking, notifications, and profile.
4. **Provider Operating OS**: Multi-tenant business workspace with role-based staff access, live calendar scheduling, service & product catalogue, and analytics.
5. **Platform Governance Admin**: Isolated cross-tenant oversight, provider verification, review moderation, financial payouts, and system diagnostics.

---

## 2. Route Registry Matrix

| Path | Domain / Layout | Access Policy | Primary Responsibility |
| :--- | :--- | :--- | :--- |
| `/` | Customer | Public | Marketplace discovery home, search hero, categories |
| `/discover` | Customer | Public | Interactive map, radius filtering, sorted provider list |
| `/business/:slug` | Customer | Public | Published provider storefront, verified hours, staff, services |
| `/book/:slug` | Customer | Public / Auth | 5-step calendar booking flow with 5-min slot hold guarantee |
| `/favorites` | Customer | Customer | Persisted customer saved providers |
| `/appointments` | Customer | Customer | Current and past appointment management |
| `/orders` | Customer | Customer | Product retail purchase history and fulfillment state |
| `/profile` | Customer | Customer | Customer profile, notifications, and payment methods |
| `/checkout` | Customer | Customer | Multi-product retail basket checkout |
| `/login` | Auth | Public / Guest | Authentication credentials submission |
| `/register` | Auth | Public / Guest | User and customer account creation |
| `/forgot-password` | Auth | Public / Guest | Token-based recovery link dispatch |
| `/reset-password` | Auth | Public / Guest | Password reset token verification |
| `/provider/dashboard` | Provider | Provider / Staff | Operational KPI summary & quick actions |
| `/provider/calendar` | Provider | Provider / Staff | Multi-specialist schedule grid & appointment adjustments |
| `/provider/bookings` | Provider | Provider / Staff | Appointment records, filtering, and status transitions |
| `/provider/customers` | Provider | Provider / Staff | Tenant customer directory and visit history |
| `/provider/services` | Provider | Provider Admin | Service menu, duration, buffers, pricing, category config |
| `/provider/products` | Provider | Provider Admin | Boutique retail inventory & stock alerts |
| `/provider/staff` | Provider | Provider Admin | Staff roster, working hours, and commission splits |
| `/provider/locations` | Provider | Provider Admin | Branch addresses, timezone IDs, and coordinates |
| `/provider/orders` | Provider | Provider / Staff | Retail pickup and dispatch order fulfillment |
| `/provider/payments` | Provider | Provider Owner | Revenue balances, platform fee reconciliation, payouts |
| `/provider/analytics` | Provider | Provider Admin | Booking volume, staff utilization, and customer retention |
| `/provider/team` | Provider | Provider Owner | Staff invitations, role assignments, and permission boundaries |
| `/provider/settings` | Provider | Provider Admin | Business profile, cancellation policies, deposit config |
| `/admin` | Admin | Platform Admin | System-wide metrics, Gross Booking Volume, platform take |
| `/admin/providers` | Admin | Platform Admin | Provider onboarding review, approval, and verification |
| `/admin/moderation` | Admin | Platform Admin | Content moderation, flagged reviews, and suspension actions |
| `/admin/payments` | Admin | Platform Admin | Escrow holdings, dispute handling, automated payout runs |
| `/admin/audit` | Admin | Platform Admin | Tamper-evident audit logging for sensitive administrative actions |
| `/admin/health` | Admin | Platform Admin | Infrastructure diagnostics, clock health, and outbox queue |

---

## 3. Strict Boundary Enforcements
- **Client Guards + Authoritative Server Verification**: Hiding a navigation item never replaces backend authorization. All `/api/v1/provider/*` and `/api/v1/admin/*` endpoints strictly validate claims and tenant scoping in ASP.NET Core middleware.
- **Tenant Context Isolation**: In provider routes, data queries are scoped by `ITenantContext`. Cross-tenant data bleed is strictly prohibited.
- **Clean Public Data Projections**: Public storefront and booking routes utilize dedicated public DTOs that never expose internal commissions, payout balances, or customer PII.
