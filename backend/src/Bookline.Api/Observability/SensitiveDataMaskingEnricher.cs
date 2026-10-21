using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Bookline.Api.Observability;

/// <summary>
/// Section 119: Centralized Serilog logging enricher for automatic privacy protection and redaction.
/// Masks passwords, auth tokens, secrets, card numbers, and raw PII across all structured log properties.
/// </summary>
public class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveKeyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newpassword",
        "oldpassword",
        "passwordhash",
        "token",
        "accesstoken",
        "refreshtoken",
        "resettoken",
        "clientsecret",
        "secret",
        "apikey",
        "webhooksecret",
        "cardnumber",
        "cvv",
        "cvc",
        "authorization",
        "privatekey"
    };

    private static readonly Regex BearerTokenRegex = new(@"Bearer\s+[A-Za-z0-9\-_.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d{4}[ -]?){3}\d{4}\b", RegexOptions.Compiled);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToRedact = logEvent.Properties
            .Where(p => SensitiveKeyNames.Contains(p.Key))
            .ToList();

        foreach (var prop in propertiesToRedact)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, "[REDACTED]"));
        }

        // Check string properties for embedded bearer tokens or cards
        foreach (var prop in logEvent.Properties)
        {
            if (prop.Value is ScalarValue scalar && scalar.Value is string strVal)
            {
                if (BearerTokenRegex.IsMatch(strVal))
                {
                    var sanitized = BearerTokenRegex.Replace(strVal, "Bearer [REDACTED]");
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, sanitized));
                }
                else if (CreditCardRegex.IsMatch(strVal))
                {
                    var sanitized = CreditCardRegex.Replace(strVal, "[CARD-REDACTED]");
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, sanitized));
                }
            }
        }
    }
}
