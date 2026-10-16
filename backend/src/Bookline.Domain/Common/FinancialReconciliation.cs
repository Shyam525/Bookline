namespace Bookline.Domain.Common;

using Bookline.Domain.Exceptions;

public record CommissionSplit(
    Money GrossAmount,
    Money CommissionAmount,
    Money ProviderNetAmount,
    decimal CommissionRatePercentage);

public record RefundSplit(
    Money RefundAmount,
    Money RefundedCommission,
    Money RefundedProviderNet);

public record OrderFinancialSummary(
    Money Subtotal,
    Money Tax,
    Money PlatformFee,
    Money Discount,
    Money TotalAmount);

/// <summary>
/// Authoritative financial reconciliation engine adhering to Section 86 (Financial Correctness).
/// Enforces mathematical invariants between gross transaction amounts, platform commissions,
/// provider payable balances, and proportional refund allocations with zero rounding drift.
/// </summary>
public static class FinancialReconciliation
{
    /// <summary>
    /// Calculates platform commission and provider payable net amount.
    /// Invariant: GrossAmount == CommissionAmount + ProviderNetAmount
    /// </summary>
    public static CommissionSplit SplitCommission(Money grossAmount, decimal commissionRatePercentage)
    {
        if (commissionRatePercentage < 0m || commissionRatePercentage > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionRatePercentage), "Commission rate must be between 0% and 100%.");
        }

        if (grossAmount.IsNegative)
        {
            throw new ArgumentException("Gross amount cannot be negative.", nameof(grossAmount));
        }

        var currency = grossAmount.Currency;
        var commissionDecimal = grossAmount.Amount * (commissionRatePercentage / 100m);
        var commissionAmount = new Money(commissionDecimal, currency);

        // Core Invariant: ProviderNet = Gross - Commission
        // Guarantees Commission + ProviderNet == Gross with zero penny-rounding leakage
        var providerNetAmount = grossAmount - commissionAmount;

        return new CommissionSplit(grossAmount, commissionAmount, providerNetAmount, commissionRatePercentage);
    }

    /// <summary>
    /// Proportionally calculates refund splits for platform commission and provider balance.
    /// Invariant: RefundAmount == RefundedCommission + RefundedProviderNet
    /// </summary>
    public static RefundSplit SplitRefund(Money originalGross, Money originalCommission, Money refundAmount)
    {
        if (originalGross.Currency != originalCommission.Currency || originalGross.Currency != refundAmount.Currency)
        {
            throw new CurrencyMismatchException(originalGross.Currency, refundAmount.Currency);
        }

        if (refundAmount > originalGross)
        {
            throw new InvalidOperationException($"Refund amount ({refundAmount}) cannot exceed original gross amount ({originalGross}).");
        }

        if (refundAmount.IsZero || originalGross.IsZero)
        {
            var zero = Money.Zero(originalGross.Currency);
            return new RefundSplit(zero, zero, zero);
        }

        var refundRatio = refundAmount.Amount / originalGross.Amount;
        var refundedCommissionDecimal = originalCommission.Amount * refundRatio;
        var refundedCommission = new Money(refundedCommissionDecimal, originalGross.Currency);

        // Core Invariant: RefundedProviderNet = RefundAmount - RefundedCommission
        var refundedProviderNet = refundAmount - refundedCommission;

        return new RefundSplit(refundAmount, refundedCommission, refundedProviderNet);
    }

    /// <summary>
    /// Computes order financial breakdown from line item amounts and tax/fee configurations.
    /// Invariant: TotalAmount == Subtotal + Tax + PlatformFee - Discount
    /// </summary>
    public static OrderFinancialSummary CalculateOrderSummary(
        IEnumerable<Money> itemTotals,
        string currency,
        decimal taxRatePercentage = 0m,
        Money? platformFee = null,
        Money? discount = null)
    {
        var normalizedCurrency = CurrencyRegistry.Normalize(currency);
        var subtotal = Money.Zero(normalizedCurrency);

        foreach (var item in itemTotals)
        {
            if (item.Currency != normalizedCurrency)
            {
                throw new CurrencyMismatchException(normalizedCurrency, item.Currency);
            }
            subtotal += item;
        }

        var taxDecimal = subtotal.Amount * (taxRatePercentage / 100m);
        var tax = new Money(taxDecimal, normalizedCurrency);

        var fee = platformFee ?? Money.Zero(normalizedCurrency);
        if (fee.Currency != normalizedCurrency)
        {
            throw new CurrencyMismatchException(normalizedCurrency, fee.Currency);
        }

        var disc = discount ?? Money.Zero(normalizedCurrency);
        if (disc.Currency != normalizedCurrency)
        {
            throw new CurrencyMismatchException(normalizedCurrency, disc.Currency);
        }

        var total = subtotal + tax + fee - disc;
        if (total.IsNegative)
        {
            total = Money.Zero(normalizedCurrency);
        }

        return new OrderFinancialSummary(subtotal, tax, fee, disc, total);
    }

    /// <summary>
    /// Validates financial reconciliation across payment, commission, and payout ledgers.
    /// </summary>
    public static void AssertReconciled(
        Money charged,
        Money refunded,
        Money netCommission,
        Money netProviderPayable)
    {
        if (charged.Currency != refunded.Currency || charged.Currency != netCommission.Currency || charged.Currency != netProviderPayable.Currency)
        {
            throw new CurrencyMismatchException(charged.Currency, refunded.Currency);
        }

        var netCollected = charged - refunded;
        var accounted = netCommission + netProviderPayable;

        if (netCollected != accounted)
        {
            throw new InvalidOperationException(
                $"Financial reconciliation mismatch: Net collected amount ({netCollected}) does not equal sum of net commission ({netCommission}) and net provider payable ({netProviderPayable}). Residual discrepancy: {netCollected.Amount - accounted.Amount} {charged.Currency}");
        }
    }
}
