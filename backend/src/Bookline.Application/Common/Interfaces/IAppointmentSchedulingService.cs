namespace Bookline.Application.Common.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IAppointmentSchedulingService
{
    Task ValidateScheduleEligibilityAsync(
        Guid tenantId,
        Guid staffId,
        Guid serviceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? locationId = null,
        Guid? excludeBookingId = null,
        bool allowPast = false,
        CancellationToken cancellationToken = default);
}

