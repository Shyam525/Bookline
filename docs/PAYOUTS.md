# Provider Payouts & Disbursements

## 1. Overview
The Payout subsystem manages the disbursement of net earnings from the marketplace to individual provider bank accounts or digital wallets.

## 2. Payout Balance Lifecycle (Section 89)
Provider earnings transition through three balances on the `Tenant` entity:
1. **Pending Balance** (`PendingPayoutBalance`):
   - Escrowed funds from recent bookings or pending orders awaiting service fulfillment.
2. **Available Balance** (`AvailablePayoutBalance`):
   - Completed services and delivered orders past the dispute or refund window.
   - Eligible for immediate disbursement.
3. **Paid Out Balance** (`PaidOutBalance`):
   - Total cumulative historical disbursements successfully transferred to the provider.

## 3. Payout Execution & Abstraction
The `IPayoutProvider` abstraction supports mock and production payout gateways (e.g., Stripe Connect, Razorpay Route, Bank IMPS/NEFT):

```csharp
public interface IPayoutProvider
{
    Task<PayoutResult> DisbursePayoutAsync(ProcessPayoutRequest request, CancellationToken cancellationToken = default);
}

public record ProcessPayoutRequest(
    Guid TenantId,
    decimal Amount,
    string Currency,
    string Method,
    string? DestinationAccount
);

public record PayoutResult(
    bool Success,
    Guid? PayoutId,
    string? TransactionReference,
    string? ErrorMessage
);
```

## 4. Audit & History
Every payout generates a persistent `Payout` record in the database:
- `Id`: Unique payout identifier.
- `TenantId`: Scoped to provider.
- `Amount`: Net disbursement amount.
- `Currency`: Typically INR or USD.
- `Status`: `Pending`, `Processing`, `Completed`, or `Failed`.
- `PaidAtUtc`: Exact timestamp of bank settlement.
