namespace Bookline.Application.Analytics.DTOs;

public record AnalyticsSummaryDto(
    decimal TotalRevenue,
    decimal TotalDeposits,
    int TotalBookings,
    int CompletedBookings,
    int CancelledBookings,
    decimal AverageTicketSize,
    decimal OccupancyRatePercentage,
    string TopStaffName,
    string TopServiceName
);

public record RevenueChartDataPoint(
    string PeriodLabel,
    decimal Revenue,
    decimal Deposits,
    int BookingsCount
);

public record StaffUtilizationDto(
    Guid StaffId,
    string StaffName,
    int TotalAppointments,
    double TotalHoursBooked,
    decimal RevenueGenerated,
    double UtilizationPercentage
);

public record ServicePerformanceDto(
    Guid ServiceId,
    string ServiceName,
    int TotalBookings,
    decimal RevenueGenerated,
    double SharePercentage
);
