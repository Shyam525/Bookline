namespace Bookline.Domain.Common;

using System.Globalization;
using System.Text.Json.Serialization;
using Bookline.Domain.Exceptions;

/// <summary>
/// Decimal-safe, currency-aware Money value object adhering to Section 86 (Financial Correctness).
/// Prevents floating point errors, rejects cross-currency operations without explicit conversion,
/// and enforces ISO 4217 minor-unit scales (e.g. 0 decimals for JPY, 2 for USD/INR/EUR, 3 for BHD/KWD).
/// </summary>
public readonly record struct Money : IComparable<Money>, IComparable, IFormattable
{
    public decimal Amount { get; }
    public string Currency { get; }

    [JsonConstructor]
    public Money(decimal amount, string currency)
    {
        Currency = CurrencyRegistry.Normalize(currency);
        Amount = CurrencyRegistry.Round(amount, Currency);
    }

    public static Money Zero(string currency) => new(0m, currency);

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public static Money FromMinorUnits(long minorUnits, string currency)
    {
        var normalized = CurrencyRegistry.Normalize(currency);
        var decimalAmount = CurrencyRegistry.FromMinorUnits(minorUnits, normalized);
        return new Money(decimalAmount, normalized);
    }

    public long ToMinorUnits() => CurrencyRegistry.ToMinorUnits(Amount, Currency);

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public Money Abs() => new(Math.Abs(Amount), Currency);

    // Operator overloads
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal scalar)
    {
        return new Money(money.Amount * scalar, money.Currency);
    }

    public static Money operator *(decimal scalar, Money money)
    {
        return new Money(money.Amount * scalar, money.Currency);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0m)
        {
            throw new DivideByZeroException("Cannot divide Money amount by zero.");
        }

        return new Money(money.Amount / divisor, money.Currency);
    }

    public static Money operator -(Money money)
    {
        return new Money(-money.Amount, money.Currency);
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new CurrencyMismatchException(left.Currency, right.Currency);
        }
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is not Money other)
        {
            throw new ArgumentException("Object must be of type Money.", nameof(obj));
        }

        return CompareTo(other);
    }

    public string Format(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var scale = CurrencyRegistry.GetScale(Currency);
        var symbol = CurrencyRegistry.GetSymbol(Currency);
        var format = $"N{scale}";
        return $"{symbol}{Amount.ToString(format, culture)}";
    }

    public override string ToString()
    {
        var scale = CurrencyRegistry.GetScale(Currency);
        return $"{Amount.ToString($"F{scale}", CultureInfo.InvariantCulture)} {Currency}";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format)) return ToString();
        return Amount.ToString(format, formatProvider) + " " + Currency;
    }
}
