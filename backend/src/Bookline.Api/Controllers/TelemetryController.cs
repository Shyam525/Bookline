using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TelemetryController> _logger;
    private readonly IWebHostEnvironment _environment;

    private static readonly Regex BotUserAgentRegex = new(
        @"(Googlebot|bingbot|Slurp|DuckDuckBot|Baiduspider|YandexBot|Sogou|Exabot|facebot|facebookexternalhit|ia_archiver|curl|python-requests|PostmanRuntime|Playwright)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VisitorSessionRegex = new(
        @"^[a-zA-Z0-9_\-]{8,64}$",
        RegexOptions.Compiled);

    public TelemetryController(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<TelemetryController> logger,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Records a privacy-conscious provider workspace activity event.
    /// Excludes automated tests, background workers, and admin traffic from pilot telemetry.
    /// </summary>
    [HttpPost("provider-activity")]
    [Authorize]
    public async Task<IActionResult> RecordProviderActivity(
        [FromBody] RecordProviderActivityRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new { Error = "Valid tenant workspace context is required." });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";

        // Exclude PlatformAdmin actions from provider business activity
        if (string.Equals(role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { Ignored = true, Reason = "PlatformAdmin activity excluded from provider metrics." });
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        var isDevOrTest = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");
        var isAutomatedTest = BotUserAgentRegex.IsMatch(userAgent);
        if (isDevOrTest && Request.Headers.ContainsKey("X-Automated-Test"))
        {
            isAutomatedTest = true;
        }

        // Allowed meaningful provider activity event types (Section 122 & Phase 7.6)
        var allowedEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProviderWorkspaceOpened",
            "ProviderCalendarViewed",
            "ManualAppointmentCreated",
            "AppointmentUpdated",
            "AppointmentCompleted",
            "ServiceConfigurationUpdated",
            "StaffScheduleUpdated"
        };

        if (!allowedEvents.Contains(request.EventType))
        {
            return BadRequest(new { Error = $"Unsupported provider activity event: {request.EventType}" });
        }

        // Deduplicate ProviderWorkspaceOpened and ProviderCalendarViewed within a 15-minute sliding window per user
        var fifteenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-15);
        if (request.EventType.Equals("ProviderWorkspaceOpened", StringComparison.OrdinalIgnoreCase) ||
            request.EventType.Equals("ProviderCalendarViewed", StringComparison.OrdinalIgnoreCase))
        {
            var alreadyRecorded = await _dbContext.ProviderActivityEvents
                .AnyAsync(e => e.TenantId == tenantId &&
                               e.UserId == userId &&
                               e.EventType == request.EventType &&
                               e.TimestampUtc >= fifteenMinutesAgo, cancellationToken);

            if (alreadyRecorded)
            {
                return Ok(new { Recorded = false, Deduplicated = true });
            }
        }

        var activityEvent = new ProviderActivityEvent
        {
            TenantId = tenantId,
            UserId = userId,
            UserRole = role,
            EventType = request.EventType,
            CorrelationId = HttpContext.TraceIdentifier,
            MetadataJson = request.MetadataJson ?? "{}",
            TimestampUtc = DateTimeOffset.UtcNow,
            ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent,
            IsAutomatedOrTest = isAutomatedTest
        };

        _dbContext.ProviderActivityEvents.Add(activityEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Recorded = true, EventId = activityEvent.Id });
    }

    /// <summary>
    /// Records top-of-funnel storefront events (Visitor -> AvailabilityViewed).
    /// HoldCreated and BookingConfirmed are generated authoritatively server-side only.
    /// Enforces privacy-conscious pseudonymous session tracking, deduplication, and exclusion of bot/preview/test traffic.
    /// </summary>
    [HttpPost("storefront-funnel")]
    [AllowAnonymous]
    [EnableRateLimiting("discovery-limit")]
    public async Task<IActionResult> RecordStorefrontFunnel(
        [FromBody] RecordStorefrontFunnelRequest request,
        CancellationToken cancellationToken)
    {
        // Server-side authoritative guard: clients may only record top-of-funnel steps
        var clientAllowedSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Visitor",
            "AvailabilityViewed"
        };

        if (string.Equals(request.Step, "HoldCreated", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Step, "BookingConfirmed", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Error = $"Step '{request.Step}' is authoritative and must not be submitted by client. It is generated server-side during the booking flow." });
        }

        if (!clientAllowedSteps.Contains(request.Step))
        {
            return BadRequest(new { Error = $"Invalid funnel step: {request.Step}. Clients may only submit 'Visitor' or 'AvailabilityViewed'." });
        }

        Tenant? tenant = null;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            tenant = await _dbContext.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        }
        else if (request.TenantId.HasValue && request.TenantId.Value != Guid.Empty)
        {
            tenant = await _dbContext.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == request.TenantId.Value, cancellationToken);
        }

        if (tenant == null)
        {
            return NotFound(new { Error = "Business storefront tenant not found." });
        }

        // Pseudonymous visitor session resolution and validation
        var visitorSessionId = request.VisitorSessionId;
        if (string.IsNullOrWhiteSpace(visitorSessionId))
        {
            visitorSessionId = Request.Headers["X-Visitor-Session-Id"].FirstOrDefault();
        }
        if (string.IsNullOrWhiteSpace(visitorSessionId) && Request.Cookies.TryGetValue("bookline_vid", out var cookieVid))
        {
            visitorSessionId = cookieVid;
        }

        if (!string.IsNullOrWhiteSpace(visitorSessionId))
        {
            if (!VisitorSessionRegex.IsMatch(visitorSessionId))
            {
                return BadRequest(new { Error = "VisitorSessionId must be 8-64 characters and contain only alphanumeric characters, underscores, or hyphens." });
            }
        }
        else
        {
            visitorSessionId = Guid.NewGuid().ToString("N");
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        var isDevOrTest = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");

        // Bot / Automated-Test header spoofing protection
        var isBot = BotUserAgentRegex.IsMatch(userAgent);
        if (isDevOrTest && Request.Headers.ContainsKey("X-Automated-Test"))
        {
            isBot = true;
        }

        // Preview header spoofing protection: in production, verify authenticated provider role for this tenant
        var isInternalPreview = false;
        if (isDevOrTest)
        {
            isInternalPreview = Request.Headers.ContainsKey("X-Internal-Preview") || request.IsInternalPreview == true;
        }
        else
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var isProvider = string.Equals(userRole, "BusinessOwner", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(userRole, "Staff", StringComparison.OrdinalIgnoreCase);
                if (isProvider && _tenantContext.IsResolved && _tenantContext.TenantId == tenant.Id)
                {
                    isInternalPreview = true;
                }
            }
        }

        var dateKey = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Deduplication for top-of-funnel steps: Visitor and AvailabilityViewed are counted once per session per day
        var exists = await _dbContext.StorefrontFunnelEvents
            .AnyAsync(f => f.TenantId == tenant.Id &&
                           f.Step == request.Step &&
                           f.VisitorSessionId == visitorSessionId &&
                           f.DateKey == dateKey, cancellationToken);

        if (exists)
        {
            return Ok(new { Recorded = false, Deduplicated = true, VisitorSessionId = visitorSessionId });
        }

        var funnelEvent = new StorefrontFunnelEvent
        {
            TenantId = tenant.Id,
            Step = request.Step,
            VisitorSessionId = visitorSessionId,
            RelatedHoldId = null,
            RelatedBookingId = null,
            IsAutomatedOrBot = isBot,
            IsInternalPreview = isInternalPreview,
            DateKey = dateKey,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _dbContext.StorefrontFunnelEvents.Add(funnelEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Recorded = true, EventId = funnelEvent.Id, VisitorSessionId = visitorSessionId });
    }

    /// <summary>
    /// Computes accurate pilot weekly metrics using half-open reporting intervals [weekStartUtc, weekEndUtc).
    /// Strictly filters out synthetic demo businesses and automated test artifacts.
    /// </summary>
    [HttpGet("pilot-metrics")]
    [Authorize]
    public async Task<IActionResult> GetPilotMetrics(
        [FromQuery] DateTimeOffset? weekStart = null,
        [FromQuery] DateTimeOffset? weekEnd = null,
        CancellationToken cancellationToken = default)
    {
        var startUtc = weekStart ?? DateTimeOffset.UtcNow.AddDays(-7);
        var endUtc = weekEnd ?? DateTimeOffset.UtcNow;

        if (endUtc <= startUtc)
        {
            return BadRequest(new { Error = "weekEnd must be strictly greater than weekStart." });
        }

        var isPlatformAdmin = User.IsInRole("PlatformAdmin");
        var tenantId = _tenantContext.TenantId;

        // Retrieve all demo tenants to ensure zero synthetic data pollution in pilot metrics
        var demoTenantIds = await _dbContext.Tenants.IgnoreQueryFilters()
            .Where(t => t.IsDemo)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        // 1. Weekly Active Providers (WAP): Distinct tenant IDs with >= 1 meaningful non-test provider activity in [startUtc, endUtc)
        var wapQuery = _dbContext.ProviderActivityEvents.IgnoreQueryFilters()
            .Where(e => !e.IsAutomatedOrTest &&
                        !demoTenantIds.Contains(e.TenantId) &&
                        e.TimestampUtc >= startUtc &&
                        e.TimestampUtc < endUtc);

        if (!isPlatformAdmin && tenantId != Guid.Empty)
        {
            wapQuery = wapQuery.Where(e => e.TenantId == tenantId);
        }

        var activeProviderIds = await wapQuery
            .Select(e => e.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // 2. Completed Appointments using CompletedAtUtc in [startUtc, endUtc)
        var completedQuery = _dbContext.Bookings.IgnoreQueryFilters()
            .Where(b => b.Status == BookingStatus.Completed &&
                        !demoTenantIds.Contains(b.TenantId) &&
                        b.CompletedAtUtc.HasValue &&
                        b.CompletedAtUtc.Value >= startUtc &&
                        b.CompletedAtUtc.Value < endUtc);

        if (!isPlatformAdmin && tenantId != Guid.Empty)
        {
            completedQuery = completedQuery.Where(b => b.TenantId == tenantId);
        }

        var completedBookings = await completedQuery.ToListAsync(cancellationToken);

        // 3. Appointment Sources Breakdown
        var sourceBreakdown = completedBookings
            .GroupBy(b => b.Source.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // 4. Storefront Conversion Funnel
        var funnelQuery = _dbContext.StorefrontFunnelEvents.IgnoreQueryFilters()
            .Where(f => !f.IsAutomatedOrBot &&
                        !f.IsInternalPreview &&
                        !demoTenantIds.Contains(f.TenantId) &&
                        f.TimestampUtc >= startUtc &&
                        f.TimestampUtc < endUtc);

        if (!isPlatformAdmin && tenantId != Guid.Empty)
        {
            funnelQuery = funnelQuery.Where(f => f.TenantId == tenantId);
        }

        var funnelEvents = await funnelQuery.ToListAsync(cancellationToken);

        var uniqueVisitors = funnelEvents
            .Where(f => f.Step == "Visitor")
            .Select(f => f.VisitorSessionId)
            .Distinct()
            .Count();

        var availabilityViews = funnelEvents
            .Where(f => f.Step == "AvailabilityViewed")
            .Select(f => f.VisitorSessionId)
            .Distinct()
            .Count();

        var holdsCreated = funnelEvents
            .Where(f => f.Step == "HoldCreated")
            .Count();

        var bookingsConfirmed = funnelEvents
            .Where(f => f.Step == "BookingConfirmed")
            .Count();

        var visitorToBookingConversion = uniqueVisitors > 0
            ? Math.Round((double)bookingsConfirmed / uniqueVisitors * 100, 2)
            : 0.0;

        return Ok(new
        {
            ReportingWindow = new
            {
                StartUtc = startUtc,
                EndUtc = endUtc,
                IntervalType = "HalfOpen [startUtc, endUtc)"
            },
            WeeklyActiveProviders = new
            {
                Count = activeProviderIds.Count,
                ProviderTenantIds = isPlatformAdmin ? activeProviderIds : null
            },
            CompletedAppointments = new
            {
                Total = completedBookings.Count,
                BySource = sourceBreakdown
            },
            StorefrontFunnel = new
            {
                UniqueVisitors = uniqueVisitors,
                AvailabilityViews = availabilityViews,
                HoldsCreated = holdsCreated,
                ConfirmedOnlineBookings = bookingsConfirmed,
                ConversionRatePercent = visitorToBookingConversion
            }
        });
    }

    /// <summary>
    /// Task 3: PlatformAdmin audit endpoint demonstrating tenant quarantine counts with zero PII exposure.
    /// </summary>
    [HttpGet("tenant-audit")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> GetTenantAudit(CancellationToken cancellationToken)
    {
        var totalTenants = await _dbContext.Tenants.IgnoreQueryFilters().CountAsync(cancellationToken);
        var demoTenants = await _dbContext.Tenants.IgnoreQueryFilters().CountAsync(t => t.IsDemo, cancellationToken);
        var realPilotTenants = await _dbContext.Tenants.IgnoreQueryFilters().CountAsync(t => !t.IsDemo, cancellationToken);

        return Ok(new
        {
            TotalTenants = totalTenants,
            SyntheticDemoTenants = demoTenants,
            RealPilotTenants = realPilotTenants,
            IsAuditClean = realPilotTenants == 0
        });
    }
}

public record RecordProviderActivityRequest(string EventType, string? MetadataJson = null);

public record RecordStorefrontFunnelRequest(
    string Step,
    string? Slug = null,
    Guid? TenantId = null,
    string? VisitorSessionId = null,
    Guid? RelatedHoldId = null,
    Guid? RelatedBookingId = null,
    bool? IsInternalPreview = null);
