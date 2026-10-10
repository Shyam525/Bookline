using Bookline.Application.Common.Interfaces;
using Bookline.Application.Payments.DTOs;
using Bookline.Application.Payments.Handlers;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
[EnableRateLimiting("payment-limit")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentHandlers _handlers;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IPayoutProvider _payoutProvider;
    private readonly ITenantContext _tenantContext;
    private readonly BooklineDbContext _dbContext;
    private readonly IIdempotencyService _idempotencyService;

    public PaymentsController(
        PaymentHandlers handlers,
        IPaymentProvider paymentProvider,
        IPayoutProvider payoutProvider,
        ITenantContext tenantContext,
        BooklineDbContext dbContext,
        IIdempotencyService idempotencyService)
    {
        _handlers = handlers;
        _paymentProvider = paymentProvider;
        _payoutProvider = payoutProvider;
        _tenantContext = tenantContext;
        _dbContext = dbContext;
        _idempotencyService = idempotencyService;
    }

    [HttpGet]
    public async Task<ActionResult<Bookline.Application.Common.Models.PagedResult<PaymentDto>>> GetPayments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(
            new GetPaymentsQuery(pageNumber, pageSize, status, type),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<PaymentSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await _handlers.Handle(new GetPaymentSummaryQuery(), cancellationToken);
        return Ok(summary);
    }

    [HttpPost("checkout-session")]
    public async Task<ActionResult<PaymentDto>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var idempKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            var cached = await _idempotencyService.GetExistingAsync(idempKey, "CheckoutSession", cancellationToken);
            if (cached != null)
            {
                var cachedDto = System.Text.Json.JsonSerializer.Deserialize<PaymentDto>(cached.ResponseJson);
                if (cachedDto != null) return StatusCode(cached.StatusCode, cachedDto);
            }
        }

        var payment = await _handlers.Handle(command, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "CheckoutSession",
                200,
                System.Text.Json.JsonSerializer.Serialize(payment),
                payment.TenantId,
                null,
                cancellationToken);
        }

        return Ok(payment);
    }

    [HttpPost("refund")]
    public async Task<ActionResult<PaymentDto>> ProcessRefund(
        [FromBody] ProcessRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        var idempKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            var cached = await _idempotencyService.GetExistingAsync(idempKey, "Refund", cancellationToken);
            if (cached != null)
            {
                var cachedDto = System.Text.Json.JsonSerializer.Deserialize<PaymentDto>(cached.ResponseJson);
                if (cachedDto != null) return StatusCode(cached.StatusCode, cachedDto);
            }
        }

        var refund = await _handlers.Handle(command, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "Refund",
                200,
                System.Text.Json.JsonSerializer.Serialize(refund),
                refund.TenantId,
                null,
                cancellationToken);
        }

        return Ok(refund);
    }

    [HttpPost("pos")]
    public async Task<ActionResult<PaymentDto>> RecordInStorePayment(
        [FromBody] RecordInStorePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var idempKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            var cached = await _idempotencyService.GetExistingAsync(idempKey, "POSPayment", cancellationToken);
            if (cached != null)
            {
                var cachedDto = System.Text.Json.JsonSerializer.Deserialize<PaymentDto>(cached.ResponseJson);
                if (cachedDto != null) return StatusCode(cached.StatusCode, cachedDto);
            }
        }

        var payment = await _handlers.Handle(command, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "POSPayment",
                200,
                System.Text.Json.JsonSerializer.Serialize(payment),
                payment.TenantId,
                null,
                cancellationToken);
        }

        return Ok(payment);
    }

    /// <summary>
    /// Authoritative backend gateway verification (Section 85).
    /// Frontend cannot determine successful payment; backend verification is authoritative.
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<PaymentVerificationResult>> VerifyPayment(
        [FromBody] VerifyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionReference))
        {
            return BadRequest(new { Message = "Transaction reference is required." });
        }

        var result = await _paymentProvider.VerifyPaymentAsync(request.TransactionReference, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Provider financial view: pending, available, paid (Section 89).
    /// </summary>
    [HttpGet("payout-balance")]
    public async Task<IActionResult> GetPayoutBalance(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var targetId = tenantId ?? (_tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty);
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == targetId, cancellationToken);

        if (tenant == null)
        {
            return Ok(new
            {
                PendingPayoutBalance = 0m,
                AvailablePayoutBalance = 0m,
                PaidOutBalance = 0m,
                Currency = "INR"
            });
        }

        return Ok(new
        {
            PendingPayoutBalance = tenant.PendingPayoutBalance,
            AvailablePayoutBalance = tenant.AvailablePayoutBalance,
            PaidOutBalance = tenant.PaidOutBalance,
            Currency = tenant.Currency
        });
    }

    /// <summary>
    /// Disburse payout to provider via IPayoutProvider abstraction (Section 89).
    /// </summary>
    [HttpPost("payouts")]
    public async Task<ActionResult<PayoutResult>> RequestPayout(
        [FromBody] RequestPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetId = request.TenantId ?? (_tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty);
        if (targetId == Guid.Empty)
        {
            return BadRequest(new { Message = "Provider tenant context is required." });
        }

        var result = await _payoutProvider.DisbursePayoutAsync(new ProcessPayoutRequest(
            TenantId: targetId,
            Amount: request.Amount,
            Currency: request.Currency ?? "INR",
            Method: request.Method ?? "BankTransfer",
            DestinationAccount: request.DestinationAccount
        ), cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// List historical provider payouts (Section 89).
    /// </summary>
    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var targetId = tenantId ?? (_tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty);
        var query = _dbContext.Payouts.IgnoreQueryFilters();
        if (targetId != Guid.Empty)
        {
            query = query.Where(p => p.TenantId == targetId);
        }

        var payouts = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(50)
            .Select(p => new
            {
                p.Id,
                p.TenantId,
                p.Amount,
                p.Currency,
                Status = p.Status.ToString(),
                p.Method,
                p.DestinationAccount,
                p.PaidAtUtc,
                p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(payouts);
    }
}

public record VerifyPaymentRequest(string TransactionReference);
public record RequestPayoutRequest(Guid? TenantId, decimal Amount, string? Currency, string? Method, string? DestinationAccount);
