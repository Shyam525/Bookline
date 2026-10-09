using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using NodaTime;
using Xunit;

namespace Bookline.Application.UnitTests;

public class SlotEngineTests
{
    private readonly SlotEngine _engine = new();
    private readonly DateTimeZone _zoneNy = DateTimeZoneProviders.Tzdb["America/New_York"];

    private static bool Overlaps(Interval a, Interval b) => a.Start < b.End && b.Start < a.End;

    [Fact]
    public void Compute_NoBookings_FullDay_ReturnsConsecutiveSlots()
    {
        // Arrange: 9:00 to 17:00 NY time on a normal day (July 15, 2025)
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var step = Duration.FromMinutes(30);
        var minNotice = Duration.Zero;
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, step, minNotice);

        // Assert: 8 hours = 16 30-minute slots
        Assert.Equal(16, slots.Count);
        Assert.Equal(day.At(new LocalTime(9, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].Start);
        Assert.Equal(day.At(new LocalTime(9, 30)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].End);
        Assert.Equal(day.At(new LocalTime(16, 30)).InZoneLeniently(_zoneNy).ToInstant(), slots[^1].Start);
        Assert.Equal(day.At(new LocalTime(17, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[^1].End);
    }

    [Fact]
    public void Compute_BackToBackBookings_WithBuffer_ExcludesBufferIntervals()
    {
        // Arrange: 9:00 to 17:00, Service: 60m + 15m Buffer
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.FromMinutes(15));
        var step = Duration.FromMinutes(15);

        // Booking at 10:00 - 11:00
        var bookingStart = day.At(new LocalTime(10, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var bookingEnd = day.At(new LocalTime(11, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var bookings = new[] { new Interval(bookingStart, bookingEnd) };
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, bookings, Array.Empty<Interval>(), day, _zoneNy, now, step, Duration.Zero);

        // Assert: Pre-buffer is 9:45-10:00, Post-buffer is 11:00-11:15
        // Slot 8:45-9:45 valid. Slot 9:00-10:00 INVALID (overlaps pre-buffer 9:45-10:00)
        // Slot 11:15-12:15 valid.
        Assert.DoesNotContain(slots, s => s.Start == day.At(new LocalTime(9, 0)).InZoneLeniently(_zoneNy).ToInstant());
        Assert.Contains(slots, s => s.Start == day.At(new LocalTime(11, 15)).InZoneLeniently(_zoneNy).ToInstant());
    }

    [Fact]
    public void Compute_BookingStraddlingWindowEdge_AdjustsAvailableWindow()
    {
        // Arrange: Working window 9:00 - 17:00. Booking 8:30 - 9:30
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var bookingStart = day.At(new LocalTime(8, 30)).InZoneLeniently(_zoneNy).ToInstant();
        var bookingEnd = day.At(new LocalTime(9, 30)).InZoneLeniently(_zoneNy).ToInstant();
        var bookings = new[] { new Interval(bookingStart, bookingEnd) };
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, bookings, Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(30), Duration.Zero);

        // Assert: First slot must start at 9:30
        Assert.NotEmpty(slots);
        Assert.Equal(day.At(new LocalTime(9, 30)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].Start);
    }

    [Fact]
    public void Compute_FullDayTimeOff_ReturnsEmptySlots()
    {
        // Arrange: Working window 9:00 - 17:00. Full day time off 00:00 - 23:59
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var timeOffStart = day.At(new LocalTime(0, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var timeOffEnd = day.At(new LocalTime(23, 59)).InZoneLeniently(_zoneNy).ToInstant();
        var timeOff = new[] { new Interval(timeOffStart, timeOffEnd) };
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), timeOff, day, _zoneNy, now, Duration.FromMinutes(30), Duration.Zero);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void Compute_PartialTimeOff_ExcludesTimeOffWindow()
    {
        // Arrange: Lunch break 12:00 - 13:00
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var timeOffStart = day.At(new LocalTime(12, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var timeOffEnd = day.At(new LocalTime(13, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var timeOff = new[] { new Interval(timeOffStart, timeOffEnd) };
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), timeOff, day, _zoneNy, now, Duration.FromMinutes(30), Duration.Zero);

        // Assert
        Assert.DoesNotContain(slots, s => s.Start >= timeOffStart && s.Start < timeOffEnd);
        Assert.Contains(slots, s => s.Start == day.At(new LocalTime(11, 30)).InZoneLeniently(_zoneNy).ToInstant());
        Assert.Contains(slots, s => s.Start == day.At(new LocalTime(13, 0)).InZoneLeniently(_zoneNy).ToInstant());
    }

    [Fact]
    public void Compute_NoWorkingHours_ReturnsEmptySlots()
    {
        // Arrange
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(Array.Empty<WorkingWindow>());
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(30), Duration.Zero);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void Compute_DstSpringForward_AmericaNewYork_March9_ComputesValidSlots()
    {
        // Arrange: March 9, 2025 in NY (Spring Forward: 2 AM becomes 3 AM, 23-hour day)
        var day = new LocalDate(2025, 3, 9);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(1, 0), new LocalTime(5, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);
        var now = Instant.FromUtc(2025, 3, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(60), Duration.Zero);

        // Assert: 1:00 AM - 5:00 AM local is 3 actual hours (1-2 AM, then 3-4 AM, 4-5 AM)
        Assert.Equal(3, slots.Count);
        Assert.Equal(day.At(new LocalTime(1, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].Start);
        Assert.Equal(day.At(new LocalTime(3, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[1].Start);
    }

    [Fact]
    public void Compute_DstFallBack_AmericaNewYork_Nov2_ComputesValidSlots()
    {
        // Arrange: Nov 2, 2025 in NY (Fall Back: 1 AM repeated, 25-hour day)
        var day = new LocalDate(2025, 11, 2);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);
        var now = Instant.FromUtc(2025, 11, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(60), Duration.Zero);

        // Assert: 8 consecutive 60-minute slots generated after DST shift completes
        Assert.Equal(8, slots.Count);
    }

    [Fact]
    public void Compute_MinimumNotice_DropsSlotsBeforeCutoff()
    {
        // Arrange: Day is July 15, 2025. Now is 9:15 AM NY time. MinNotice is 2 hours (cutoff 11:15 AM)
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.Zero);
        var now = day.At(new LocalTime(9, 15)).InZoneLeniently(_zoneNy).ToInstant();
        var minNotice = Duration.FromHours(2);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(30), minNotice);

        // Assert: Cutoff is 11:15 AM. First slot must be at 11:30 AM
        Assert.All(slots, s => Assert.True(s.Start >= now + minNotice));
        Assert.Equal(day.At(new LocalTime(11, 30)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].Start);
    }

    [Fact]
    public void Compute_ServiceDurationExceedsWindow_ReturnsEmptySlots()
    {
        // Arrange: 2-hour service, 1-hour working window (9:00 - 10:00)
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(10, 0)) });
        var service = new ServiceInfo(Duration.FromHours(2), Duration.Zero);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(30), Duration.Zero);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void Compute_BufferExceedsGap_DiscardsGap()
    {
        // Arrange: 15-minute gap between bookings, but service buffer is 20 minutes
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(17, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(30), Duration.FromMinutes(20));

        var booking1End = day.At(new LocalTime(10, 0)).InZoneLeniently(_zoneNy).ToInstant();
        var booking2Start = day.At(new LocalTime(10, 15)).InZoneLeniently(_zoneNy).ToInstant();

        var bookings = new[]
        {
            new Interval(day.At(new LocalTime(9, 0)).InZoneLeniently(_zoneNy).ToInstant(), booking1End),
            new Interval(booking2Start, day.At(new LocalTime(17, 0)).InZoneLeniently(_zoneNy).ToInstant())
        };
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, bookings, Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(15), Duration.Zero);

        // Assert: Gap 10:00-10:15 cannot fit 30m service + 20m buffer
        Assert.Empty(slots);
    }

    [Fact]
    public void Compute_StepSizeSmallerThanDuration_GeneratesOverlappingValidStartOptions()
    {
        // Arrange: 60-minute service with 15-minute step size in 9:00-11:00 window
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(11, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(15), Duration.Zero);

        // Assert: 9:00, 9:15, 9:30, 9:45, 10:00 (5 start times)
        Assert.Equal(5, slots.Count);
        Assert.Equal(day.At(new LocalTime(9, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[0].Start);
        Assert.Equal(day.At(new LocalTime(9, 15)).InZoneLeniently(_zoneNy).ToInstant(), slots[1].Start);
        Assert.Equal(day.At(new LocalTime(10, 0)).InZoneLeniently(_zoneNy).ToInstant(), slots[4].Start);
    }

    [Fact]
    public void Compute_MultipleSplitWorkingWindows_GeneratesSlotsPerWindow()
    {
        // Arrange: Split windows 9:00-12:00 and 14:00-17:00
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[]
        {
            new WorkingWindow(new LocalTime(9, 0), new LocalTime(12, 0)),
            new WorkingWindow(new LocalTime(14, 0), new LocalTime(17, 0))
        });
        var service = new ServiceInfo(Duration.FromMinutes(60), Duration.Zero);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        // Act
        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, Duration.FromMinutes(60), Duration.Zero);

        // Assert: 3 slots in morning + 3 slots in afternoon = 6 slots
        Assert.Equal(6, slots.Count);
        Assert.DoesNotContain(slots, s => s.Start >= day.At(new LocalTime(12, 0)).InZoneLeniently(_zoneNy).ToInstant() &&
                                           s.Start < day.At(new LocalTime(14, 0)).InZoneLeniently(_zoneNy).ToInstant());
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    public void PropertyTest_NoReturnedSlotOverlapsAnyBookingOrTimeOff(int stepMinutes)
    {
        // Arrange: Complex scenario with 2 bookings and 1 time off
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(8, 0), new LocalTime(18, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(45), Duration.FromMinutes(15));
        var step = Duration.FromMinutes(stepMinutes);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        var booking1 = new Interval(
            day.At(new LocalTime(10, 0)).InZoneLeniently(_zoneNy).ToInstant(),
            day.At(new LocalTime(11, 30)).InZoneLeniently(_zoneNy).ToInstant());

        var timeOff = new Interval(
            day.At(new LocalTime(13, 0)).InZoneLeniently(_zoneNy).ToInstant(),
            day.At(new LocalTime(14, 0)).InZoneLeniently(_zoneNy).ToInstant());

        var booking2 = new Interval(
            day.At(new LocalTime(15, 30)).InZoneLeniently(_zoneNy).ToInstant(),
            day.At(new LocalTime(16, 30)).InZoneLeniently(_zoneNy).ToInstant());

        var bookings = new[] { booking1, booking2 };
        var timeOffs = new[] { timeOff };

        // Act
        var slots = _engine.Compute(service, schedule, bookings, timeOffs, day, _zoneNy, now, step, Duration.Zero);

        // Assert Property: Every slot must be disjoint from (booking + buffer) and timeOff
        foreach (var slot in slots)
        {
            var slotInterval = new Interval(slot.Start, slot.End);

            // 1. Check time off overlap
            foreach (var to in timeOffs)
            {
                Assert.False(Overlaps(slotInterval, to), $"Slot {slot.Start}-{slot.End} overlaps time off {to.Start}-{to.End}");
            }

            // 2. Check booking + buffer overlap
            foreach (var b in bookings)
            {
                var bufferedBooking = new Interval(b.Start - service.Buffer, b.End + service.Buffer);
                Assert.False(Overlaps(slotInterval, bufferedBooking), $"Slot {slot.Start}-{slot.End} overlaps buffered booking {bufferedBooking.Start}-{bufferedBooking.End}");
            }
        }
    }

    [Fact]
    public void Compute_Section54_SlotInvalidWhenCompleteRequiredIntervalExceedsWindow()
    {
        // Specification Section 54:
        // Business: 09:00–18:00
        // Service: 45 minutes
        // Buffer: 10 minutes
        // Required: 55 minutes
        // A 17:30 slot is invalid (and 17:15 is invalid because 17:15 + 55m = 18:10 > 18:00).
        var day = new LocalDate(2025, 7, 15);
        var schedule = new StaffSchedule(new[] { new WorkingWindow(new LocalTime(9, 0), new LocalTime(18, 0)) });
        var service = new ServiceInfo(Duration.FromMinutes(45), Duration.FromMinutes(10));
        var step = Duration.FromMinutes(15);
        var now = Instant.FromUtc(2025, 7, 1, 0, 0);

        var slots = _engine.Compute(service, schedule, Array.Empty<Interval>(), Array.Empty<Interval>(), day, _zoneNy, now, step, Duration.Zero);

        // 17:30 slot must NOT exist
        var slotAt1730 = day.At(new LocalTime(17, 30)).InZoneLeniently(_zoneNy).ToInstant();
        Assert.DoesNotContain(slots, s => s.Start == slotAt1730);

        // 17:15 slot must NOT exist (17:15 + 55 min = 18:10 > 18:00)
        var slotAt1715 = day.At(new LocalTime(17, 15)).InZoneLeniently(_zoneNy).ToInstant();
        Assert.DoesNotContain(slots, s => s.Start == slotAt1715);

        // 17:00 slot IS valid (17:00 + 45m = 17:45 service end, + 10m buffer = 17:55 <= 18:00)
        var slotAt1700 = day.At(new LocalTime(17, 0)).InZoneLeniently(_zoneNy).ToInstant();
        Assert.Contains(slots, s => s.Start == slotAt1700);
        var lastSlot = slots[^1];
        Assert.Equal(slotAt1700, lastSlot.Start);
        Assert.Equal(day.At(new LocalTime(17, 45)).InZoneLeniently(_zoneNy).ToInstant(), lastSlot.End);
    }
}
