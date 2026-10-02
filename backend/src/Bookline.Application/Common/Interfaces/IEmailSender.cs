namespace Bookline.Application.Common.Interfaces;

using Bookline.Domain.Entities;

public interface IEmailSender
{
    Task SendBookingConfirmationEmailAsync(
        Booking booking,
        Service service,
        Staff staff,
        Customer customer,
        Tenant tenant,
        string rescheduleToken,
        string cancelToken,
        CancellationToken cancellationToken = default);
}
