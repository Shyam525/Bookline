namespace Bookline.Application.Common.Interfaces;

public record ProcessPaymentRequest(
    Guid TenantId,
    Guid? CustomerId,
    Guid? BookingId,
    Guid? OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod, // "Card", "Stripe", "Razorpay", "POS", "Demo"
    string? PaymentToken = null,
    string? Notes = null
);

public record PaymentResult(
    bool Success,
    Guid PaymentId,
    string TransactionReference,
    string Status,
    string? ReceiptUrl,
    string? ErrorMessage
);

public record ProcessRefundRequest(
    Guid PaymentId,
    decimal Amount,
    string Reason
);

public record RefundResult(
    bool Success,
    string RefundReference,
    string Status,
    string? ErrorMessage
);

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default);
    Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request, CancellationToken cancellationToken = default);
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
    Guid PayoutId,
    string Reference,
    string Status,
    string? ErrorMessage
);

public interface IPayoutProvider
{
    Task<PayoutResult> DisbursePayoutAsync(ProcessPayoutRequest request, CancellationToken cancellationToken = default);
}
