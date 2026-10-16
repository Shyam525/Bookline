namespace Bookline.Domain.Common;

using System.Collections.Concurrent;
using System.Globalization;
using Bookline.Domain.Exceptions;

public record CurrencySpecification(string Code, int Scale, string Symbol, string EnglishName);

/// <summary>
/// Authoritative ISO 4217 currency registry adhering to Section 86 (Financial Correctness).
/// Distinguishes currency minor-unit scales (0 for JPY, 3 for BHD/KWD, 2 for USD/INR/EUR)
/// and prevents arbitrary precision drift or accidental cross-currency calculations.
/// </summary>
public static class CurrencyRegistry
{
    private static readonly ConcurrentDictionary<string, CurrencySpecification> Currencies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = new("USD", 2, "$", "US Dollar"),
            ["EUR"] = new("EUR", 2, "€", "Euro"),
            ["GBP"] = new("GBP", 2, "£", "British Pound"),
            ["INR"] = new("INR", 2, "₹", "Indian Rupee"),
            ["CAD"] = new("CAD", 2, "CA$", "Canadian Dollar"),
            ["AUD"] = new("AUD", 2, "A$", "Australian Dollar"),
            ["SGD"] = new("SGD", 2, "S$", "Singapore Dollar"),
            ["AED"] = new("AED", 2, "AED", "UAE Dirham"),
            ["CHF"] = new("CHF", 2, "CHF", "Swiss Franc"),
            ["SEK"] = new("SEK", 2, "kr", "Swedish Krona"),
            ["NZD"] = new("NZD", 2, "NZ$", "New Zealand Dollar"),

            // Zero-decimal currencies (Scale = 0)
            ["JPY"] = new("JPY", 0, "¥", "Japanese Yen"),
            ["KRW"] = new("KRW", 0, "₩", "South Korean Won"),
            ["VND"] = new("VND", 0, "₫", "Vietnamese Dong"),
            ["CLP"] = new("CLP", 0, "CLP$", "Chilean Peso"),

            // Three-decimal currencies (Scale = 3)
            ["BHD"] = new("BHD", 3, "BD", "Bahraini Dinar"),
            ["KWD"] = new("KWD", 3, "KD", "Kuwaiti Dinar"),
            ["OMR"] = new("OMR", 3, "OMR", "Omani Rial"),
            ["JOD"] = new("JOD", 3, "JD", "Jordanian Dinar")
        };

    public const string DefaultCurrency = "USD";

    public static bool IsSupported(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        return Currencies.ContainsKey(currencyCode.Trim());
    }

    public static string Normalize(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new UnsupportedCurrencyException("<empty>");
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (!Currencies.ContainsKey(normalized))
        {
            throw new UnsupportedCurrencyException(normalized);
        }

        return normalized;
    }

    public static CurrencySpecification Get(string currencyCode)
    {
        var normalized = Normalize(currencyCode);
        return Currencies[normalized];
    }

    public static int GetScale(string currencyCode)
    {
        return Get(currencyCode).Scale;
    }

    public static string GetSymbol(string currencyCode)
    {
        return Get(currencyCode).Symbol;
    }

    /// <summary>
    /// Applies deterministic, currency-aware rounding (Banker's or AwayFromZero).
    /// Defaults to MidpointRounding.AwayFromZero for standard commercial rounding.
    /// </summary>
    public static decimal Round(decimal amount, string currencyCode, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        var scale = GetScale(currencyCode);
        return Math.Round(amount, scale, rounding);
    }

    /// <summary>
    /// Converts a decimal monetary amount to the integer minor-units representation (e.g. cents, paise, fils, yen)
    /// required by payment gateways (Stripe, Razorpay, etc.).
    /// </summary>
    public static long ToMinorUnits(decimal amount, string currencyCode)
    {
        var scale = GetScale(currencyCode);
        var factor = (decimal)Math.Pow(10, scale);
        var rounded = Math.Round(amount, scale, MidpointRounding.AwayFromZero);
        return (long)(rounded * factor);
    }

    /// <summary>
    /// Reconstitutes a decimal monetary amount from an integer minor-units value.
    /// </summary>
    public static decimal FromMinorUnits(long minorUnits, string currencyCode)
    {
        var scale = GetScale(currencyCode);
        var factor = (decimal)Math.Pow(10, scale);
        return minorUnits / factor;
    }

    public static IReadOnlyCollection<CurrencySpecification> GetAllSupported() => Currencies.Values.ToList();
}
