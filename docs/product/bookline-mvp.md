# Bookline MVP Product Definition & Scope Specification

## 1. Product Overview & Initial Market Focus

**Product**: Bookline  
**Initial Market**: Gujarat, India (Configurable launch city, initial focus on urban hubs: Ahmedabad, Surat, Vadodara, Rajkot)  
**Primary Provider Segments**:
- Salons and barbershops (unisex, men's grooming, women's salons)
- Beauty studios and independent beauty professionals (makeup artists, estheticians, nail artists)
- Spas and compatible wellness businesses (massage therapy, wellness centers)

**Product Strategy**: **Provider-Management SaaS First, Customer Booking & Marketplace Second**  
Bookline must win provider trust as an indispensable daily operational workspace before attempting to scale customer discovery. If a provider does not run their daily business through Bookline's calendar, any customer marketplace on top will display ghost availability, leading to appointment conflicts, manual cancellations, and customer churn.

---

## 2. Core Value Propositions

### 2.1 Provider Promise
> *"Manage appointments, staff, services and customers from one place, including bookings received through walk-ins, calls, WhatsApp, Instagram and online booking links."*

The provider's operational reality in Gujarat is omnichannel and fragmented:
- Customers message on WhatsApp or Instagram DM.
- Regular clients call the salon owner directly.
- Walk-in customers step into the salon without prior notice.
- Digital clients book through social bio links.

Bookline acts as the single source of truth that unifies all four demand streams into one authoritative calendar.

### 2.2 Customer Promise
> *"Discover local service providers, see their services and genuine availability, and book online."*

Customers receive real-time, genuine slot availability with instant confirmation, eliminating back-and-forth messaging, long salon waiting times, and conflicting bookings.

---

## 3. The Core Anchor: The Unified Calendar

The most critical operational workflow in Bookline is the **Unified Calendar**.

```
  [Walk-in Customer]  ───┐
  [Phone Call Client] ───┼──► [Provider Calendar UI] ──┐
  [WhatsApp / DM]     ───┘                             │
                                                       ▼
                                            ┌─────────────────────┐
                                            │ Authoritative Slot  │
                                            │   Engine (Backend)  │
                                            └──────────┬──────────┘
                                                       ▲
  [Direct Booking Link] ──► [Public Booking Flow] ─────┘
```

### Scheduling Invariants:
1. **Identical Rules**: Manual entries (walk-ins, phone calls) and online customer bookings MUST pass through the exact same server-side scheduling rules, working hours checks, and buffer validations.
2. **Unified Records**: All appointments are stored in the canonical `Bookings` table. There are no separate "offline ledger" entities.
3. **Zero Double-Booking Guarantee**: A slot reserved via online booking locks the staff member from receiving a walk-in, and a walk-in entered on the calendar immediately removes that slot from public availability.

---

## 4. Product Boundaries & Deliberate Constraints

These constraints are deliberate product decisions designed to ensure focus, speed, operational excellence, and architectural integrity:

| Area | MVP Decision | Rationale |
|---|---|---|
| **Location Count** | **One operational location per provider** | Majority of independent salons and barbershops in Gujarat operate a single physical branch. Keeps UI/UX uncluttered while schema retains `LocationId` for seamless future multi-branch expansion. |
| **Monetization Model** | **Provider SaaS Subscription Hypothesis** | Providers in this market reject unpredictable marketplace transaction fees or commission cuts on their hard-earned service revenue. Fixed predictable SaaS pricing aligns provider incentives. |
| **Marketplace Commissions** | **Strictly Excluded from MVP** | No commission cuts, split payments, or escrow ledgers. Eliminates regulatory and financial friction during early adoption. |
| **Payment Gateways** | **No Online Payment Gateways in MVP** | High payment gateway failure rates, UPI drop-offs, and merchant onboarding friction impede pilot adoption. Providers handle payments offline (Cash, Direct UPI QR, Card swipe) and log the payment method in Bookline. |
| **Customer Discovery** | **Direct Link First, Simple Local Discovery Second** | Salons drive demand through their own Instagram, Google Maps profile, and WhatsApp status links. Curated local directory exists but direct link (`/book/{slug}`) is the primary conversion engine. |
| **Mobile Platforms** | **Responsive Web / PWA Only (No Native Mobile Apps)** | Speeds up iteration, avoids app store review cycles, and enables instant accessibility on low-end Android mobile devices and salon desktop tablets. |
| **AI Features** | **Strictly Excluded** | No AI chatbots, AI scheduling assistants, or automated marketing bots. Focus on rock-solid deterministic scheduling and fast responsive UI. |
| **E-Commerce & Retail** | **Strictly Excluded from MVP Core** | No complex physical retail inventory, cart checkouts, or product shipping. Service businesses in Gujarat sell time and expertise first. |
| **Localization** | **English & Gujarati (i18n-Ready Frontend)** | Tier-2 salon staff and receptionists in Gujarat frequently operate in Gujarati. System must support bilingual interfaces with human-reviewable translation dictionaries. |
| **Launch City** | **Configurable (Not Hardcoded)** | Default launch city configured via settings (e.g. Ahmedabad), but architecture and UI must allow launching in Surat, Rajkot, or Vadodara simply by changing configuration. |

---

## 5. Core MVP User Workflows

### 5.1 Provider Journey
1. **Onboarding & Setup**:
   - Register provider account with email/phone and password.
   - Enter business profile (name, category, launch city, address, timezone default `Asia/Kolkata`).
   - Define weekly operating hours and split shifts.
   - Add staff members, assign roles, and set their individual working schedules.
   - Configure service menu (name, duration in minutes, buffer times before/after, price in INR).
2. **Daily Operations (Unified Calendar)**:
   - View day/week schedule grid broken down by staff member.
   - Log walk-in / phone appointments with 3 clicks (Customer Name, Service, Staff, Time).
   - Check-in arriving clients, mark appointments as Completed, or flag No-Shows.
   - Record offline payment method (Cash, Direct UPI, Card).
   - Block breaks, lunch hours, or unplanned time-off.
3. **Client Management (CRM)**:
   - Search client directory by name or phone.
   - View customer appointment history, past services, and personal preferences/notes.

### 5.2 Customer Journey
1. **Booking via Direct Link (`/book/{slug}`)**:
   - Customer opens provider's direct link from Instagram bio, WhatsApp, or Google Maps.
   - Selects service(s) and preferred staff member (or "Any Specialist").
   - Views genuine real-time open slots on their chosen date.
   - Selects a slot (backend acquires short-lived 10-minute hold).
   - Enters name, mobile phone number, and optional notes.
   - Submits booking $\rightarrow$ instant confirmation on screen with appointment reference code.
2. **Customer Self-Service**:
   - Views booking details and cancellation policy.
   - Cancels or reschedules appointment within provider's permitted notice window.

