namespace Bookline.Application.Payments.DTOs;

public record PaymentDto(
    Guid Id,
    Guid TenantId,
    Guid? BookingId,
    Guid? CustomerId,
    string? CustomerName,
    decimal Amount,
    string Currency,
    string PaymentType,
    string Status,
    string PaymentMethod,
    string? StripePaymentIntentId,
    string? StripeCheckoutSessionId,
    string? ReceiptUrl,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc
);

public record PaymentSummaryDto(
    decimal TotalRevenue,
    decimal TotalDeposits,
    decimal TotalRefunds,
    int TotalTransactionsCount,
    int CompletedCount,
    int PendingCount,
    int RefundedCount
);
