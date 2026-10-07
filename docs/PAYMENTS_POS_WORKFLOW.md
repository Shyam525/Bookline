# Payments & POS Transaction Workflows

## Workflow 1: Online Deposit Collection via Stripe
1. Booking engine places temporary hold on slot via `ISlotHoldService`.
2. Customer is presented with Stripe Checkout Session (`CreateCheckoutSessionCommand`).
3. Payment record created with status `Pending` and `StripeCheckoutSessionId`.
4. Upon Stripe Webhook confirmation (`payment_intent.succeeded`), booking status flips to `Confirmed`.

## Workflow 2: In-Store POS Recording
1. Staff opens `Record In-Store POS Payment` modal in `PaymentsPage.tsx`.
2. Selects method (`Cash`, `CreditCard`, `Terminal POS`, `ApplePay`).
3. System saves `Payment` record with status `Completed` and updates customer's `TotalSpentAmount`.
