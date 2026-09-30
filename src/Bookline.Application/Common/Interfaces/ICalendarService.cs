namespace Bookline.Application.Common.Interfaces;

using Bookline.Domain.Entities;

public interface ICalendarService
{
    byte[] GenerateIcsAttachment(Booking booking, Service service, Staff staff, Tenant tenant);
}
