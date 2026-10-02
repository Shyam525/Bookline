using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using NodaTime;

namespace Bookline.Application.Common.Services;

public class SlotEngine : ISlotEngine
{
    public IReadOnlyList<Slot> Compute(
        ServiceInfo svc,
        StaffSchedule schedule,
        IReadOnlyList<Interval> bookings,
        IReadOnlyList<Interval> timeOff,
        LocalDate day,
        DateTimeZone zone,
        Instant now,
        Duration step,
        Duration minimumNotice)
    {
        if (schedule.WorkingWindows.Count == 0 || svc.Duration <= Duration.Zero || step <= Duration.Zero)
        {
            return Array.Empty<Slot>();
        }

        var cutoffInstant = now + minimumNotice;

        // 1. Map working windows (LocalTime -> ZonedDateTime -> Instant Interval) for the specified day & timezone
        var windowIntervals = new List<Interval>();
        foreach (var window in schedule.WorkingWindows)
        {
            if (window.End <= window.Start) continue;

            var startInstant = day.At(window.Start).InZoneLeniently(zone).ToInstant();
            var endInstant = day.At(window.End).InZoneLeniently(zone).ToInstant();

            if (endInstant > startInstant)
            {
                windowIntervals.Add(new Interval(startInstant, endInstant));
            }
        }

        if (windowIntervals.Count == 0)
        {
            return Array.Empty<Slot>();
        }

        // 2. Build expanded busy intervals (Bookings expanded by buffers + TimeOff)
        var busyIntervals = new List<Interval>();

        foreach (var b in bookings)
        {
            var expandedStart = b.Start - svc.Buffer;
            var expandedEnd = b.End + svc.Buffer;
            if (expandedEnd > expandedStart)
            {
                busyIntervals.Add(new Interval(expandedStart, expandedEnd));
            }
        }

        foreach (var to in timeOff)
        {
            if (to.End > to.Start)
            {
                busyIntervals.Add(to);
            }
        }

        // 3. Merge overlapping / adjacent busy intervals
        var mergedBusy = MergeIntervals(busyIntervals);

        // 4. Subtract merged busy intervals from working windows to produce free intervals
        var freeIntervals = new List<Interval>();
        foreach (var window in windowIntervals)
        {
            freeIntervals.AddRange(SubtractIntervals(window, mergedBusy));
        }

        // 5. Slice free intervals into candidate slots of duration `svc.Duration` at `step` increments
        var validSlots = new List<Slot>();

        foreach (var free in freeIntervals)
        {
            var currentStart = free.Start;

            while (currentStart + svc.Duration <= free.End)
            {
                var currentEnd = currentStart + svc.Duration;

                // Minimum notice filter: Slot start must be >= now + minimumNotice
                if (currentStart >= cutoffInstant)
                {
                    validSlots.Add(new Slot(currentStart, currentEnd));
                }

                currentStart += step;
            }
        }

        return validSlots;
    }

    private static List<Interval> MergeIntervals(List<Interval> intervals)
    {
        if (intervals.Count <= 1) return intervals;

        var sorted = intervals.OrderBy(i => i.Start).ToList();
        var result = new List<Interval> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            var last = result[^1];
            var current = sorted[i];

            if (current.Start <= last.End)
            {
                var newEnd = Instant.Max(last.End, current.End);
                result[^1] = new Interval(last.Start, newEnd);
            }
            else
            {
                result.Add(current);
            }
        }

        return result;
    }

    private static List<Interval> SubtractIntervals(Interval window, List<Interval> busyList)
    {
        var result = new List<Interval>();
        var currentStart = window.Start;

        foreach (var busy in busyList)
        {
            if (busy.End <= currentStart) continue;
            if (busy.Start >= window.End) break;

            if (busy.Start > currentStart)
            {
                var freeEnd = Instant.Min(busy.Start, window.End);
                if (freeEnd > currentStart)
                {
                    result.Add(new Interval(currentStart, freeEnd));
                }
            }

            currentStart = Instant.Max(currentStart, busy.End);
            if (currentStart >= window.End) break;
        }

        if (currentStart < window.End)
        {
            result.Add(new Interval(currentStart, window.End));
        }

        return result;
    }
}
