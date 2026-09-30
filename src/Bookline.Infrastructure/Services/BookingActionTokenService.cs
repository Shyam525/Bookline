namespace Bookline.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using Bookline.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

public class BookingActionTokenService : IBookingActionTokenService
{
    private readonly IDataProtector _protector;

    public BookingActionTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Bookline.BookingActionTokens.v1");
    }

    public string GenerateToken(Guid bookingId, string action, TimeSpan validFor)
    {
        var expiryTicks = DateTimeOffset.UtcNow.Add(validFor).Ticks;
        var payload = $"{bookingId}:{action}:{expiryTicks}";
        return _protector.Protect(payload);
    }

    public bool TryValidateToken(string token, out Guid bookingId, out string action)
    {
        bookingId = Guid.Empty;
        action = string.Empty;

        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var unprotected = _protector.Unprotect(token);
            var parts = unprotected.Split(':');
            if (parts.Length != 3) return false;

            if (!Guid.TryParse(parts[0], out bookingId)) return false;
            action = parts[1];

            if (!long.TryParse(parts[2], out var expiryTicks)) return false;

            if (DateTimeOffset.UtcNow.Ticks > expiryTicks)
            {
                // Expired token
                return false;
            }

            return true;
        }
        catch
        {
            // Decryption failure / tampered token
            return false;
        }
    }
}
