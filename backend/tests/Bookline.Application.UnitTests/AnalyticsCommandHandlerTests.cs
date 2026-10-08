namespace Bookline.Application.UnitTests;

using Bookline.Application.Analytics.DTOs;
using Bookline.Application.Analytics.Handlers;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class AnalyticsCommandHandlerTests
{
    private (BooklineDbContext DbContext, TestTenantContext TenantContext) CreateDbContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .AddInterceptors(interceptor)
            .Options;

        var context = new BooklineDbContext(options, tenantContext);
        return (context, tenantContext);
    }

    [Fact]
    public async Task GetAnalyticsSummary_ShouldCalculateFinancialMetricsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var staff = new Staff { TenantId = tenantId, Name = "Elena Rostova" };
        var service = new Service { TenantId = tenantId, Name = "Balayage Hair Color", Price = 200m };
        db.Staff.Add(staff);
        db.Services.Add(service);

        var b1 = new Booking(tenantId, staff.Id, service.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-2).AddHours(1));
        b1.Confirm();
        b1.Complete();

        var b2 = new Booking(tenantId, staff.Id, service.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1).AddHours(1));
        b2.Cancel();

        db.Bookings.AddRange(b1, b2);

        db.Payments.Add(new Payment { TenantId = tenantId, BookingId = b1.Id, Amount = 200m, PaymentType = PaymentType.FullPayment, Status = PaymentStatus.Completed, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) });
        await db.SaveChangesAsync();

        var handlers = new AnalyticsHandlers(db, context);

        var summary = await handlers.Handle(new GetAnalyticsSummaryQuery());

        Assert.NotNull(summary);
        Assert.Equal(200m, summary.TotalRevenue);
        Assert.Equal(2, summary.TotalBookings);
        Assert.Equal(1, summary.CompletedBookings);
        Assert.Equal(1, summary.CancelledBookings);
        Assert.Equal(200m, summary.AverageTicketSize);
        Assert.Equal("Elena Rostova", summary.TopStaffName);
        Assert.Equal("Balayage Hair Color", summary.TopServiceName);
    }

    [Fact]
    public async Task GetRevenueChart_ShouldReturnHistoricalTrendPoints()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        db.Payments.Add(new Payment
        {
            TenantId = tenantId,
            Amount = 150m,
            PaymentType = PaymentType.FullPayment,
            Status = PaymentStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow.Date
        });
        await db.SaveChangesAsync();

        var handlers = new AnalyticsHandlers(db, context);

        var chart = await handlers.Handle(new GetRevenueChartQuery("7days"));

        Assert.NotNull(chart);
        Assert.Equal(8, chart.Count); // 7 days back + today
        var todayPoint = chart.Last();
        Assert.Equal(150m, todayPoint.Revenue);
    }

    [Fact]
    public async Task ExportAnalyticsCsv_ShouldFormatValidCsvData()
    {
        var tenantId = Guid.NewGuid();
        var (db, context) = CreateDbContext(tenantId);

        var handlers = new AnalyticsHandlers(db, context);

        var csv = await handlers.Handle(new ExportAnalyticsCsvQuery());

        Assert.NotNull(csv);
        Assert.Contains("Metric,Value", csv);
        Assert.Contains("Total Revenue,$0", csv);
        Assert.Contains("Staff Name,Appointments,Hours Booked,Revenue,Utilization %", csv);
    }
}
