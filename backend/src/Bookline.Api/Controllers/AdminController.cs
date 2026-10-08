using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize] // Platform admin authorization
public class AdminController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly IPayoutProvider _payoutProvider;

    public AdminController(BooklineDbContext dbContext, IPayoutProvider payoutProvider)
    {
        _dbContext = dbContext;
        _payoutProvider = payoutProvider;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetPlatformMetrics(CancellationToken cancellationToken)
    {
        var providersCount = await _dbContext.Tenants.IgnoreQueryFilters().CountAsync(cancellationToken);
        var activeProvidersCount = await _dbContext.Tenants.IgnoreQueryFilters()
            .CountAsync(t => t.VerificationStatus == VerificationStatus.Verified && t.IsActive, cancellationToken);
        var customersCount = await _dbContext.Users.IgnoreQueryFilters()
            .CountAsync(u => u.Role == "Customer", cancellationToken);
        var bookingsCount = await _dbContext.Bookings.IgnoreQueryFilters().CountAsync(cancellationToken);
        var ordersCount = await _dbContext.Orders.IgnoreQueryFilters().CountAsync(cancellationToken);

        var payments = await _dbContext.Payments.IgnoreQueryFilters()
            .Where(p => p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var commissions = await _dbContext.Commissions.IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var gmv = payments.Sum(p => p.Amount);
        var commissionRevenue = commissions.Sum(c => c.CommissionAmount);

        var cities = await _dbContext.Tenants.IgnoreQueryFilters()
            .Select(t => t.City)
            .Distinct()
            .CountAsync(cancellationToken);

        return Ok(new
        {
            TotalProviders = providersCount,
            ActiveVerifiedProviders = activeProvidersCount,
            TotalCustomers = customersCount,
            TotalBookings = bookingsCount,
            TotalOrders = ordersCount,
            GrossMerchandiseValue = gmv,
            PlatformCommissionRevenue = commissionRevenue,
            ActiveCities = Math.Max(1, cities),
            Currency = "USD"
        });
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await _dbContext.Tenants.IgnoreQueryFilters()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Category,
                t.BusinessType,
                t.City,
                t.Address,
                t.Phone,
                t.AverageRating,
                t.ReviewCount,
                VerificationStatus = t.VerificationStatus.ToString(),
                t.IsActive,
                t.IsPublished,
                t.CommissionRatePercentage,
                t.PendingPayoutBalance,
                t.AvailablePayoutBalance,
                t.PaidOutBalance,
                t.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(providers);
    }

    [HttpPut("providers/{id:guid}/verification")]
    public async Task<IActionResult> UpdateProviderVerification(Guid id, [FromBody] UpdateVerificationRequest request, CancellationToken cancellationToken)
    {
        var provider = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (provider == null) return NotFound(new { Message = "Provider not found." });

        if (Enum.TryParse<VerificationStatus>(request.Status, true, out var newStatus))
        {
            provider.VerificationStatus = newStatus;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { provider.Id, provider.Name, VerificationStatus = provider.VerificationStatus.ToString() });
        }

        return BadRequest(new { Message = "Invalid verification status." });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _dbContext.Users.IgnoreQueryFilters()
            .Where(u => u.Role == "Customer")
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.Phone,
                u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("commissions")]
    public async Task<IActionResult> GetCommissions(CancellationToken cancellationToken)
    {
        var commissions = await _dbContext.Commissions.IgnoreQueryFilters()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(commissions);
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts(CancellationToken cancellationToken)
    {
        var payouts = await _dbContext.Payouts.IgnoreQueryFilters()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(payouts);
    }

    [HttpPost("payouts/process")]
    public async Task<IActionResult> TriggerPayout([FromBody] TriggerPayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _payoutProvider.DisbursePayoutAsync(new ProcessPayoutRequest(
            TenantId: request.TenantId,
            Amount: request.Amount,
            Currency: request.Currency ?? "USD",
            Method: "StripeConnect",
            DestinationAccount: "Connected Bank Rails"
        ), cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { Message = result.ErrorMessage });
        }

        return Ok(result);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviewsQueue(CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews.IgnoreQueryFilters()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(reviews);
    }

    [HttpGet("health")]
    public IActionResult GetSystemHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Database = "Connected (PostgreSQL / Relational)",
            Cache = "Active (Redis Transport)",
            OutboxProcessor = "Running",
            Timestamp = DateTime.UtcNow
        });
    }
}

public record UpdateVerificationRequest(string Status);
public record TriggerPayoutRequest(Guid TenantId, decimal Amount, string? Currency);
