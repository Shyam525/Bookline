namespace Bookline.Domain.Exceptions;

public class CurrencyMismatchException : DomainException
{
    public string ExpectedCurrency { get; }
    public string ActualCurrency { get; }

    public CurrencyMismatchException(string expectedCurrency, string actualCurrency)
        : base($"Currency mismatch: operation requires identical currencies, but encountered '{expectedCurrency}' and '{actualCurrency}'. Cross-currency operations require explicit exchange rate conversion.")
    {
        ExpectedCurrency = expectedCurrency;
        ActualCurrency = actualCurrency;
    }
}

public class UnsupportedCurrencyException : DomainException
{
    public string CurrencyCode { get; }

    public UnsupportedCurrencyException(string currencyCode)
        : base($"Unsupported currency code: '{currencyCode}'. Currency must be a valid ISO 4217 code supported by Bookline.")
    {
        CurrencyCode = currencyCode;
    }
}
