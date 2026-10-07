namespace Bookline.Api.Controllers;

using Bookline.Application.Payments.DTOs;
using Bookline.Application.Payments.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly PaymentHandlers _handlers;

    public PaymentsController(PaymentHandlers handlers)
    {
        _handlers = handlers;
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
        var payment = await _handlers.Handle(command, cancellationToken);
        return Ok(payment);
    }

    [HttpPost("refund")]
    public async Task<ActionResult<PaymentDto>> ProcessRefund(
        [FromBody] ProcessRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        var refund = await _handlers.Handle(command, cancellationToken);
        return Ok(refund);
    }

    [HttpPost("pos")]
    public async Task<ActionResult<PaymentDto>> RecordInStorePayment(
        [FromBody] RecordInStorePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var payment = await _handlers.Handle(command, cancellationToken);
        return Ok(payment);
    }
}
