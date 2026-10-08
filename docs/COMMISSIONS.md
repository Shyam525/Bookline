# MARKETPLACE ECONOMICS & COMMISSIONS

## 1. Platform Economics Model
Bookline operates as a commercial marketplace where platform revenue is generated via a transparent take-rate model:
- **Default Take-Rate**: 10% (configurable per tenant or category).
- **Split Formula**:
  - `Gross Amount`: Paid by customer.
  - `Bookline Commission`: `Gross Amount * (Rate / 100)`.
  - `Provider Payable`: `Gross Amount - Bookline Commission`.

## 2. Server-Authoritative Calculation
Commission rules and calculation logic are strictly enforced backend-side:
- Domain method: `Commission.Calculate(grossAmount, ratePercentage)`.
- Prevents tampering or client-side fee modification.
- Both appointment transactions and retail commerce orders generate commission audit rows in the database.

## 3. Payout Balances
- **Available Payout Balance**: Accumulated funds from completed appointments and fulfilled retail orders ready for provider settlement.
- **Paid Out Lifetime**: Disbursed sum sent to the provider's registered bank or payment rail.
- **Disbursement API**: Platform administrators trigger payouts via `/api/v1/admin/payouts/process`, updating vendor ledger balances transactionally.
