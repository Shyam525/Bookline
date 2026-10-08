namespace Bookline.Api.Controllers;

using Bookline.Application.Analytics.DTOs;
using Bookline.Application.Analytics.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsHandlers _handlers;

    public AnalyticsController(AnalyticsHandlers handlers)
    {
        _handlers = handlers;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(new GetAnalyticsSummaryQuery(startDate, endDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenue-chart")]
    public async Task<ActionResult<List<RevenueChartDataPoint>>> GetRevenueChart(
        [FromQuery] string period = "30days",
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(new GetRevenueChartQuery(period), cancellationToken);
        return Ok(result);
    }

    [HttpGet("staff-utilization")]
    public async Task<ActionResult<List<StaffUtilizationDto>>> GetStaffUtilization(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(new GetStaffUtilizationQuery(startDate, endDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("service-performance")]
    public async Task<ActionResult<List<ServicePerformanceDto>>> GetServicePerformance(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(new GetServicePerformanceQuery(startDate, endDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var csv = await _handlers.Handle(new ExportAnalyticsCsvQuery(startDate, endDate), cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"bookline_analytics_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
