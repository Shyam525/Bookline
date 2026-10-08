namespace Bookline.Application.Analytics.Handlers;

using Bookline.Application.Analytics.DTOs;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public record GetAnalyticsSummaryQuery(DateTime? StartDate = null, DateTime? EndDate = null);
public record GetRevenueChartQuery(string Period = "30days");
public record GetStaffUtilizationQuery(DateTime? StartDate = null, DateTime? EndDate = null);
public record GetServicePerformanceQuery(DateTime? StartDate = null, DateTime? EndDate = null);
public record ExportAnalyticsCsvQuery(DateTime? StartDate = null, DateTime? EndDate = null);

public class AnalyticsHandlers
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AnalyticsHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<AnalyticsSummaryDto> Handle(GetAnalyticsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var start = query.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var end = query.EndDate ?? DateTime.UtcNow;

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartUtc >= start && b.StartUtc <= end)
            .ToListAsync(cancellationToken);

        var payments = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAtUtc >= start && p.CreatedAtUtc <= end && p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var totalRevenue = payments.Where(p => p.PaymentType != PaymentType.Refund).Sum(p => p.Amount);
        var totalDeposits = payments.Where(p => p.PaymentType == PaymentType.Deposit).Sum(p => p.Amount);

        var totalBookings = bookings.Count;
        var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed);
        var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);

        var averageTicket = completedBookings > 0 ? totalRevenue / completedBookings : 0m;
        var occupancy = totalBookings > 0 ? (completedBookings / (double)totalBookings) * 100.0 : 0.0;

        // Top Staff
        var staffStats = bookings
            .GroupBy(b => b.StaffId)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefault();

        string topStaffName = "None";
        if (staffStats != null)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == staffStats.StaffId, cancellationToken);
            if (staff != null) topStaffName = staff.Name;
        }

        // Top Service
        var serviceStats = bookings
            .GroupBy(b => b.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefault();

        string topServiceName = "None";
        if (serviceStats != null)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceStats.ServiceId, cancellationToken);
            if (service != null) topServiceName = service.Name;
        }

        return new AnalyticsSummaryDto(
            TotalRevenue: totalRevenue,
            TotalDeposits: totalDeposits,
            TotalBookings: totalBookings,
            CompletedBookings: completedBookings,
            CancelledBookings: cancelledBookings,
            AverageTicketSize: Math.Round(averageTicket, 2),
            OccupancyRatePercentage: (decimal)Math.Round(occupancy, 1),
            TopStaffName: topStaffName,
            TopServiceName: topServiceName
        );
    }

    public async Task<List<RevenueChartDataPoint>> Handle(GetRevenueChartQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var days = query.Period.ToLower() switch
        {
            "7days" => 7,
            "14days" => 14,
            _ => 30
        };

        var start = DateTime.UtcNow.Date.AddDays(-days);

        var payments = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAtUtc >= start && p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartUtc >= start)
            .ToListAsync(cancellationToken);

        var chartData = new List<RevenueChartDataPoint>();

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var dateStr = date.ToString("MMM dd");

            var dayPayments = payments.Where(p => p.CreatedAtUtc.Date == date).ToList();
            var dayBookings = bookings.Where(b => b.StartUtc.Date == date).ToList();

            var rev = dayPayments.Where(p => p.PaymentType != PaymentType.Refund).Sum(p => p.Amount);
            var dep = dayPayments.Where(p => p.PaymentType == PaymentType.Deposit).Sum(p => p.Amount);

            chartData.Add(new RevenueChartDataPoint(
                PeriodLabel: dateStr,
                Revenue: rev,
                Deposits: dep,
                BookingsCount: dayBookings.Count
            ));
        }

        return chartData;
    }

    public async Task<List<StaffUtilizationDto>> Handle(GetStaffUtilizationQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var start = query.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var end = query.EndDate ?? DateTime.UtcNow;

        var staffMembers = await _context.Staff
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync(cancellationToken);

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartUtc >= start && b.StartUtc <= end)
            .ToListAsync(cancellationToken);

        var payments = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAtUtc >= start && p.CreatedAtUtc <= end && p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var result = new List<StaffUtilizationDto>();

        foreach (var staff in staffMembers)
        {
            var staffBookings = bookings.Where(b => b.StaffId == staff.Id).ToList();
            var count = staffBookings.Count;

            // Calculate hours booked
            double hours = staffBookings.Sum(b => (b.EndUtc - b.StartUtc).TotalHours);

            // Assume standard 160 working hours / month per staff for utilization math
            double util = Math.Min(100.0, (hours / 160.0) * 100.0);

            var staffBookingIds = staffBookings.Select(b => b.Id).ToHashSet();
            var revenue = payments.Where(p => p.BookingId.HasValue && staffBookingIds.Contains(p.BookingId.Value)).Sum(p => p.Amount);

            result.Add(new StaffUtilizationDto(
                StaffId: staff.Id,
                StaffName: staff.Name,
                TotalAppointments: count,
                TotalHoursBooked: Math.Round(hours, 1),
                RevenueGenerated: revenue,
                UtilizationPercentage: Math.Round(util, 1)
            ));
        }

        return result.OrderByDescending(r => r.RevenueGenerated).ToList();
    }

    public async Task<List<ServicePerformanceDto>> Handle(GetServicePerformanceQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var start = query.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var end = query.EndDate ?? DateTime.UtcNow;

        var services = await _context.Services
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartUtc >= start && b.StartUtc <= end)
            .ToListAsync(cancellationToken);

        var totalBookingsCount = bookings.Count;
        var result = new List<ServicePerformanceDto>();

        foreach (var service in services)
        {
            var sBookings = bookings.Where(b => b.ServiceId == service.Id).ToList();
            var count = sBookings.Count;
            var revenue = count * service.Price;
            double share = totalBookingsCount > 0 ? (count / (double)totalBookingsCount) * 100.0 : 0.0;

            result.Add(new ServicePerformanceDto(
                ServiceId: service.Id,
                ServiceName: service.Name,
                TotalBookings: count,
                RevenueGenerated: revenue,
                SharePercentage: Math.Round(share, 1)
            ));
        }

        return result.OrderByDescending(s => s.TotalBookings).ToList();
    }

    public async Task<string> Handle(ExportAnalyticsCsvQuery query, CancellationToken cancellationToken = default)
    {
        var summary = await Handle(new GetAnalyticsSummaryQuery(query.StartDate, query.EndDate), cancellationToken);
        var staff = await Handle(new GetStaffUtilizationQuery(query.StartDate, query.EndDate), cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Revenue,${summary.TotalRevenue}");
        sb.AppendLine($"Total Deposits,${summary.TotalDeposits}");
        sb.AppendLine($"Total Bookings,{summary.TotalBookings}");
        sb.AppendLine($"Completed Bookings,{summary.CompletedBookings}");
        sb.AppendLine($"Average Ticket Size,${summary.AverageTicketSize}");
        sb.AppendLine($"Occupancy Rate,{summary.OccupancyRatePercentage}%");
        sb.AppendLine($"Top Staff,{summary.TopStaffName}");
        sb.AppendLine($"Top Service,{summary.TopServiceName}");
        sb.AppendLine();
        sb.AppendLine("Staff Name,Appointments,Hours Booked,Revenue,Utilization %");
        foreach (var s in staff)
        {
            sb.AppendLine($"\"{s.StaffName}\",{s.TotalAppointments},{s.TotalHoursBooked},${s.RevenueGenerated},{s.UtilizationPercentage}%");
        }

        return sb.ToString();
    }
}
