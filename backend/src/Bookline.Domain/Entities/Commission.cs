using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class Commission : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PaymentId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? BookingId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionRatePercentage { get; set; } = 10.00m;
    public decimal CommissionAmount { get; set; }
    public decimal ProviderNetAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Collected"; // Pending, Collected, Refunded
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public static Commission Calculate(Guid tenantId, decimal grossAmount, decimal ratePercentage, string currency = "USD", Guid? paymentId = null, Guid? bookingId = null, Guid? orderId = null)
    {
        var normalizedCurrency = CurrencyRegistry.Normalize(currency);
        var normalizedGross = CurrencyRegistry.Round(grossAmount, normalizedCurrency);
        var commissionAmt = CurrencyRegistry.Round(normalizedGross * (ratePercentage / 100m), normalizedCurrency);
        // Reconciliation invariant: GrossAmount = CommissionAmount + ProviderNetAmount
        var netAmt = Math.Max(0m, normalizedGross - commissionAmt);

        return new Commission
        {
            TenantId = tenantId,
            PaymentId = paymentId,
            BookingId = bookingId,
            OrderId = orderId,
            GrossAmount = normalizedGross,
            CommissionRatePercentage = ratePercentage,
            CommissionAmount = commissionAmt,
            ProviderNetAmount = netAmt,
            Currency = normalizedCurrency,
            Status = "Collected",
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
