using Bookline.Domain.Common;
using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class Payout : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? Reference { get; set; }
    public string Method { get; set; } = "StripeConnect"; // StripeConnect, RazorpayRoute, BankTransfer
    public string? DestinationAccount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }

    public void MarkPaid(string reference)
    {
        Status = PayoutStatus.Paid;
        Reference = reference;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PayoutStatus.Failed;
        Reference = reason;
    }
}
