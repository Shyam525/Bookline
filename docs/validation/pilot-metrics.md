# Bookline Pilot Performance Metrics & Validation Framework

## 1. Pilot Evaluation Overview

The Bookline pilot program tests whether a provider-management SaaS platform tailored for salons, barbershops, beauty studios, and spas in Gujarat creates indispensable operational value.

The pilot evaluates two core hypotheses:
1. **Operational Utility Hypothesis**: Salon owners and staff will actively use Bookline's unified calendar to log daily walk-ins, phone bookings, and manage schedules.
2. **Online Adoption Hypothesis**: Customers will successfully use direct provider booking links (`/book/{slug}`) shared via WhatsApp and Instagram bio to book appointments, eliminating phone tag and scheduling conflicts.

---

## 2. Core Quantitative Pilot Metrics

| Metric Name | Formula / Measurement | Minimum Viable Target (Pilot) | Stretch Goal | Why It Matters |
|---|---|---|---|---|
| **Provider Onboarding Completion Rate** | $\frac{\text{Providers completing step 3 (Services \& Staff)}}{\text{Total registered provider accounts}} \times 100$ | $\ge 70\%$ | $\ge 85\%$ | Measures whether onboarding friction is low enough for non-technical salon operators. |
| **Daily Active Calendar Usage (DACU)** | $\frac{\text{Days with } \ge 3 \text{ appointments logged or modified}}{\text{Total operational days per week}}$ | $\ge 5 \text{ days / week}$ | $6 \text{ days / week}$ | Validates whether Bookline is an active operational tool or abandoned after setup. |
| **Manual / Walk-in Booking Logging Ratio** | $\frac{\text{Manual walk-in \& phone bookings logged on calendar}}{\text{Total estimated daily client volume}}$ | $\ge 50\%$ | $\ge 80\%$ | Proves the calendar reflects physical floor reality, preventing phantom availability. |
| **Direct Link Booking Volume** | Count of completed customer bookings originated via `/book/{slug}` | $\ge 15 \text{ bookings / provider / week}$ | $\ge 40 \text{ bookings}$ | Proves direct links generate real customer booking demand without a massive marketplace. |
| **Zero Double-Booking Incidents** | Count of simultaneous overlapping bookings for the same stylist in PostgreSQL | **$0$ incidents ($100\%$ integrity)** | **$0$ incidents** | Core technical integrity metric. A single double-booking ruins salon trust. |
| **No-Show Rate Reduction** | $\frac{\text{No-Show status bookings}}{\text{Total confirmed bookings}} \times 100$ | $\le 12\%$ | $\le 7\%$ | Validates whether automated reminder notifications improve customer attendance. |
| **Average Slot Utilization** | $\frac{\text{Total booked service hours}}{\text{Total scheduled staff working hours}} \times 100$ | $45\% - 65\%$ | $> 70\%$ | Measures salon productivity and schedule optimization. |
| **Bilingual Gujarati Engagement** | Percentage of provider staff toggling UI language to Gujarati | Tracking metric ($\ge 25\%$) | $\ge 40\%$ | Evaluates actual demand for regional language localization among salon assistants. |

---

## 3. Qualitative Evaluation Checkpoints (Weekly Founder Review)

During the 4-week pilot cohort across 10–15 partner salons in Gujarat:

### Week 1: Setup & Initial Floor Training
- Can the salon owner add their entire service menu and 3–5 stylists in under 15 minutes?
- Does the receptionist understand how to create a walk-in appointment with fewer than 4 clicks?
- Did any staff member report confusion around buffer times or split shifts?

### Week 2: First Direct Link Customers
- Did the salon place the Bookline booking link in their Instagram bio and WhatsApp Business greeting?
- Did customers booking through the link show up at the correct time?
- Did any walk-in conflict with an online customer?

### Week 3: Payment Logging & Daily Reckoning
- Are receptionists logging whether customers paid via Cash, UPI QR, or Card upon completion?
- Does the daily revenue summary on Bookline match the cash drawer and UPI business account totals?
- Does the owner review the daily dashboard summary at closing time?

### Week 4: Willingness to Pay & Conversion Gate
- Ask the owner: *"If Bookline stopped working tomorrow, how disappointed would you be (Very Disappointed, Somewhat Disappointed, Not Disappointed)?"*
  - **Target**: $\ge 40\%$ "Very Disappointed" (Sean Ellis PMF benchmark).
- Willingness to convert to paid subscription:
  - Offer month 2 at ₹999/month.
  - **Conversion Target**: $\ge 30\%$ of pilot salons agree to pay for continued service.

---

## 4. Telemetry & Reporting Requirements

The system must log the following discrete audit and observability events without exposing customer PII:
1. `ProviderRegisteredEvent`
2. `OperatingHoursConfiguredEvent`
3. `StaffCreatedEvent`, `ServiceCreatedEvent`
4. `AppointmentCreatedEvent` (origin: `ManualWalkIn`, `PhoneCall`, `DirectLink`)
5. `AppointmentStatusChangedEvent` (`CheckedIn`, `Completed`, `Cancelled`, `NoShow`)
6. `PaymentRecordedEvent` (method: `Cash`, `UPI`, `Card`)
7. `SlotHoldAcquiredEvent`, `SlotHoldExpiredEvent`
8. `LanguageChangedEvent` (`en` $\leftrightarrow$ `gu`)

These events provide real, data-grounded metrics for the founder to evaluate pilot progress and plan expansion.

