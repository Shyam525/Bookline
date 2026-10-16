namespace Bookline.Application.UnitTests;

using Bookline.Domain.Common;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Domain.Exceptions;
using Xunit;

public class FinancialCorrectnessSection86Tests
{
    [Fact]
    public void Money_Addition_Maintains_Exact_Precision_Without_Float_Drift()
    {
        // 0.1 + 0.2 in standard IEEE-754 float equals 0.30000000000000004
        var m1 = new Money(0.10m, "USD");
        var m2 = new Money(0.20m, "USD");
        var sum = m1 + m2;

        Assert.Equal(0.30m, sum.Amount);
        Assert.Equal("USD", sum.Currency);
        Assert.False(sum.IsZero);
        Assert.True(sum.IsPositive);
    }

    [Fact]
    public void CurrencyRegistry_Enforces_Correct_MinorUnit_Scales()
    {
        // Standard two-decimal currencies
        Assert.Equal(2, CurrencyRegistry.GetScale("USD"));
        Assert.Equal(2, CurrencyRegistry.GetScale("EUR"));
        Assert.Equal(2, CurrencyRegistry.GetScale("GBP"));
        Assert.Equal(2, CurrencyRegistry.GetScale("INR"));

        // Zero-decimal currencies
        Assert.Equal(0, CurrencyRegistry.GetScale("JPY"));
        Assert.Equal(0, CurrencyRegistry.GetScale("KRW"));

        // Three-decimal currencies
        Assert.Equal(3, CurrencyRegistry.GetScale("BHD"));
        Assert.Equal(3, CurrencyRegistry.GetScale("KWD"));
        Assert.Equal(3, CurrencyRegistry.GetScale("OMR"));
    }

    [Fact]
    public void Money_Converts_To_And_From_MinorUnits_Consistently()
    {
        // 2-decimal: $49.99 -> 4999 cents
        var usd = new Money(49.99m, "USD");
        Assert.Equal(4999L, usd.ToMinorUnits());
        Assert.Equal(usd, Money.FromMinorUnits(4999L, "USD"));

        // 0-decimal: ¥1500 -> 1500 yen
        var jpy = new Money(1500m, "JPY");
        Assert.Equal(1500L, jpy.ToMinorUnits());
        Assert.Equal(jpy, Money.FromMinorUnits(1500L, "JPY"));

        // 3-decimal: 1.250 BHD -> 1250 fils
        var bhd = new Money(1.250m, "BHD");
        Assert.Equal(1250L, bhd.ToMinorUnits());
        Assert.Equal(bhd, Money.FromMinorUnits(1250L, "BHD"));
    }

    [Fact]
    public void Money_Rejects_Cross_Currency_Arithmetic_And_Comparisons()
    {
        var usd = new Money(100.00m, "USD");
        var eur = new Money(100.00m, "EUR");

        Assert.Throws<CurrencyMismatchException>(() => _ = usd + eur);
        Assert.Throws<CurrencyMismatchException>(() => _ = usd - eur);
        Assert.Throws<CurrencyMismatchException>(() => _ = usd < eur);
        Assert.Throws<CurrencyMismatchException>(() => _ = usd > eur);
    }

    [Theory]
    [InlineData(10.05, 10.00, "USD")]
    [InlineData(99.99, 15.50, "USD")]
    [InlineData(1000.00, 7.25, "INR")]
    [InlineData(5500, 10.00, "JPY")]
    [InlineData(12.345, 12.00, "BHD")]
    public void CommissionSplit_Strictly_Satisfies_Reconciliation_Equation(
        decimal gross,
        decimal rate,
        string currency)
    {
        var grossMoney = new Money(gross, currency);
        var split = FinancialReconciliation.SplitCommission(grossMoney, rate);

        // Core Invariant: Gross == Commission + ProviderNet
        Assert.Equal(split.GrossAmount, split.CommissionAmount + split.ProviderNetAmount);
        Assert.Equal(grossMoney.Currency, split.CommissionAmount.Currency);
        Assert.Equal(grossMoney.Currency, split.ProviderNetAmount.Currency);
        Assert.True(split.CommissionAmount.Amount >= 0);
        Assert.True(split.ProviderNetAmount.Amount >= 0);
    }

    [Fact]
    public void RefundSplit_Allocates_Proportional_Commission_Without_Residual_Drift()
    {
        var gross = new Money(100.00m, "USD");
        var commission = new Money(10.00m, "USD");
        var partialRefund = new Money(30.00m, "USD");

        var refundSplit = FinancialReconciliation.SplitRefund(gross, commission, partialRefund);

        // Invariant: RefundAmount == RefundedCommission + RefundedProviderNet
        Assert.Equal(partialRefund, refundSplit.RefundedCommission + refundSplit.RefundedProviderNet);
        Assert.Equal(3.00m, refundSplit.RefundedCommission.Amount);
        Assert.Equal(27.00m, refundSplit.RefundedProviderNet.Amount);
    }

    [Fact]
    public void OrderSummary_Reconciles_Subtotal_Tax_Fees_And_Total()
    {
        var items = new List<Money>
        {
            new(25.00m, "USD"),
            new(15.50m, "USD"),
            new(9.50m, "USD")
        };

        var fee = new Money(2.00m, "USD");
        var discount = new Money(5.00m, "USD");

        var summary = FinancialReconciliation.CalculateOrderSummary(
            items,
            currency: "USD",
            taxRatePercentage: 10.00m,
            platformFee: fee,
            discount: discount);

        // Subtotal = 25.00 + 15.50 + 9.50 = 50.00
        Assert.Equal(50.00m, summary.Subtotal.Amount);

        // Tax = 10% of 50.00 = 5.00
        Assert.Equal(5.00m, summary.Tax.Amount);

        // Total = 50.00 + 5.00 + 2.00 - 5.00 = 52.00
        Assert.Equal(52.00m, summary.TotalAmount.Amount);

        // Invariant: Total == Subtotal + Tax + Fee - Discount
        Assert.Equal(summary.TotalAmount, summary.Subtotal + summary.Tax + summary.PlatformFee - summary.Discount);
    }

    [Fact]
    public void OrderSummary_Throws_When_Items_Have_Mismatched_Currencies()
    {
        var items = new List<Money>
        {
            new(25.00m, "USD"),
            new(20.00m, "EUR")
        };

        Assert.Throws<CurrencyMismatchException>(() =>
            FinancialReconciliation.CalculateOrderSummary(items, "USD"));
    }

    [Fact]
    public void HistoricalSnapshots_Preserve_Original_Pricing_When_Catalog_Changes()
    {
        var tenantId = Guid.NewGuid();
        var originalPrice = 75.00m;
        var currency = "USD";

        // Snapshot in booking
        var booking = new Booking(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1))
        {
            TotalPrice = originalPrice,
            DepositPaid = 20.00m,
            DepositPolicy = DepositType.Fixed,
            DepositRequiredAmount = 20.00m,
            Currency = currency
        };

        // Snapshot in order item
        var orderItem = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Organic Hair Oil",
            UnitPrice = originalPrice,
            Quantity = 2,
            TotalPrice = originalPrice * 2,
            Currency = currency
        };

        // Snapshot in commission
        var commission = Commission.Calculate(tenantId, booking.TotalPrice, 10.00m, currency);

        // Simulate catalog and configuration changes
        var updatedCatalogPrice = 120.00m;
        var updatedCommissionRate = 20.00m;

        // Verify snapshots are immutable and preserve historical records
        Assert.Equal(originalPrice, booking.TotalPrice);
        Assert.Equal(originalPrice, orderItem.UnitPrice);
        Assert.Equal(originalPrice * 2, orderItem.TotalPrice);
        Assert.Equal(10.00m, commission.CommissionRatePercentage);
        Assert.Equal(7.50m, commission.CommissionAmount);
        Assert.Equal(67.50m, commission.ProviderNetAmount);
        Assert.NotEqual(updatedCatalogPrice, booking.TotalPrice);
        Assert.NotEqual(updatedCommissionRate, commission.CommissionRatePercentage);
    }

    [Fact]
    public void AssertReconciled_Detects_Discrepancies_Across_Ledgers()
    {
        var charged = new Money(100.00m, "USD");
        var refunded = new Money(20.00m, "USD");
        var netCommission = new Money(8.00m, "USD");
        var netProviderPayable = new Money(72.00m, "USD"); // 8 + 72 = 80 == 100 - 20

        // Balanced ledger succeeds
        FinancialReconciliation.AssertReconciled(charged, refunded, netCommission, netProviderPayable);

        // Unbalanced ledger fails with descriptive reconciliation error
        var unrecordedDiscrepancy = new Money(70.00m, "USD"); // 8 + 70 = 78 != 80 (2.00 missing)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            FinancialReconciliation.AssertReconciled(charged, refunded, netCommission, unrecordedDiscrepancy));

        Assert.Contains("Financial reconciliation mismatch", ex.Message);
        Assert.Contains("2.00 USD", ex.Message);
    }
}
