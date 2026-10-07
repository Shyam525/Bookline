using Bookline.Domain.Common;
using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class Payment : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BookingId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentType PaymentType { get; set; } = PaymentType.FullPayment;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Stripe;
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
