namespace Bookline.Application.Common.Interfaces;

public interface IBookingActionTokenService
{
    string GenerateToken(Guid bookingId, string action, TimeSpan validFor);
    bool TryValidateToken(string token, out Guid bookingId, out string action);
}
