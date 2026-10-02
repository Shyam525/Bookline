using NodaTime;

namespace Bookline.Application.Common.Models;

public record ServiceInfo(Duration Duration, Duration Buffer);

public record WorkingWindow(LocalTime Start, LocalTime End);

public record StaffSchedule(IReadOnlyList<WorkingWindow> WorkingWindows);

public record Slot(Instant Start, Instant End);
