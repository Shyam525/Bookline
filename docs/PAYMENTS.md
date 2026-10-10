# Payments Architecture & Gateway Processing

## 1. Overview
The Payments subsystem manages checkout sessions, in-store POS transactions, refunds, authoritatively verified webhook events, platform commissions, and provider payouts.

## 2. Gateway Abstraction & Core Entities
- **Payment Entity** (`Bookline.Domain.Entities.Payment`):
  - Records transaction status: `Pending`, `Completed`, `Failed`, `Refunded`.
  - Payment methods: `Stripe`, `Razorpay`, `Cash`, `Card`, `UPI`.
  - Payment types: `Deposit`, `FullPrepayment`, `InStorePOS`, `Refund`.
  - Authoritative reference: `StripePaymentIntentId` or gateway transaction reference.

## 3. Webhook Ingestion & Replay Protection (Section 132)
- Inbound gateway webhooks are received at `POST /api/v1/payments/webhook`.
- Every webhook payload contains an `EventId` or `TransactionReference`.
- **Idempotency Verification**:
  - The `IdempotencyService` checks if `EventId` has already been processed.
  - If already processed: Returns `DuplicateAcknowledged` with `200 OK`, without creating duplicate payments or re-triggering order status changes.
  - If new: Transitions order/booking status to `Processing`/`Confirmed`, persists a single `Payment` record, and records the idempotency key with a 24-hour expiration window.

## 4. Commission & Financial Flow (Sections 85, 88, 89)
- **10% Marketplace Commission**:
  - Example: A ₹1,000 order yields ₹100 platform fee and ₹900 provider net earnings.
- **Provider Balances**:
  - Net earnings are credited to the provider's `AvailablePayoutBalance`.
  - Payouts are executed via `POST /api/v1/payments/payouts`.
