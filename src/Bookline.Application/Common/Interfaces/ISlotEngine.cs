using Bookline.Application.Common.Models;
using NodaTime;

namespace Bookline.Application.Common.Interfaces;

public interface ISlotEngine
{
    IReadOnlyList<Slot> Compute(
        ServiceInfo svc,
        StaffSchedule schedule,
        IReadOnlyList<Interval> bookings,
        IReadOnlyList<Interval> timeOff,
        LocalDate day,
        DateTimeZone zone,
        Instant now,
        Duration step,
        Duration minimumNotice);
}
