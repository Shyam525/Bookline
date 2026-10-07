namespace Bookline.Application.Payments.Handlers;

using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Payments.DTOs;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public record GetPaymentsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? StatusFilter = null,
    string? TypeFilter = null
);

public record GetPaymentSummaryQuery();

public record CreateCheckoutSessionCommand(
    Guid BookingId,
    decimal Amount,
    string Currency = "USD"
);

public record ProcessRefundCommand(
    Guid PaymentId,
    decimal RefundAmount,
    string Reason
);

public record RecordInStorePaymentCommand(
    Guid? BookingId,
    Guid? CustomerId,
    decimal Amount,
    string PaymentMethod = "Cash",
    string? Notes = null
);

public class PaymentHandlers
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PaymentHandlers(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<PaymentDto>> Handle(GetPaymentsQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var dbQuery = _context.Payments
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.StatusFilter) && Enum.TryParse<PaymentStatus>(query.StatusFilter, true, out var status))
        {
            dbQuery = dbQuery.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.TypeFilter) && Enum.TryParse<PaymentType>(query.TypeFilter, true, out var type))
        {
            dbQuery = dbQuery.Where(p => p.PaymentType == type);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var customersDict = await _context.Customers
            .Where(c => c.TenantId == tenantId)
            .ToDictionaryAsync(c => c.Id, c => c.FullName, cancellationToken);

        var items = await dbQuery
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(p => new PaymentDto(
            p.Id,
            p.TenantId,
            p.BookingId,
            p.CustomerId,
            p.CustomerId.HasValue && customersDict.TryGetValue(p.CustomerId.Value, out var name) ? name : "Walk-in Guest",
            p.Amount,
            p.Currency,
            p.PaymentType.ToString(),
            p.Status.ToString(),
            p.PaymentMethod.ToString(),
            p.StripePaymentIntentId,
            p.StripeCheckoutSessionId,
            p.ReceiptUrl,
            p.Notes,
            p.CreatedAtUtc,
            p.CompletedAtUtc
        )).ToList();

        return new PagedResult<PaymentDto>(dtos, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<PaymentSummaryDto> Handle(GetPaymentSummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        var payments = await _context.Payments
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var completed = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
        var refunded = payments.Where(p => p.Status == PaymentStatus.Refunded || p.PaymentType == PaymentType.Refund).ToList();

        var totalRevenue = completed.Where(p => p.PaymentType != PaymentType.Refund).Sum(p => p.Amount);
        var totalDeposits = completed.Where(p => p.PaymentType == PaymentType.Deposit).Sum(p => p.Amount);
        var totalRefunds = refunded.Sum(p => p.Amount);

        return new PaymentSummaryDto(
            TotalRevenue: totalRevenue,
            TotalDeposits: totalDeposits,
            TotalRefunds: totalRefunds,
            TotalTransactionsCount: payments.Count,
            CompletedCount: completed.Count,
            PendingCount: payments.Count(p => p.Status == PaymentStatus.Pending),
            RefundedCount: refunded.Count
        );
    }

    public async Task<PaymentDto> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        if (command.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.BookingId && b.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException($"Booking with ID {command.BookingId} was not found.");

        var payment = new Payment
        {
            TenantId = tenantId,
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            Amount = command.Amount,
            Currency = command.Currency,
            PaymentType = PaymentType.Deposit,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Stripe,
            StripeCheckoutSessionId = $"cs_test_{Guid.NewGuid():N}",
            StripePaymentIntentId = $"pi_test_{Guid.NewGuid():N}",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return MapPayment(payment);
    }

    public async Task<PaymentDto> Handle(ProcessRefundCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        if (command.RefundAmount <= 0)
        {
            throw new ValidationException("Refund amount must be greater than zero.");
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == command.PaymentId && p.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException($"Payment with ID {command.PaymentId} was not found.");

        payment.Status = PaymentStatus.Refunded;

        var refundRecord = new Payment
        {
            TenantId = tenantId,
            BookingId = payment.BookingId,
            CustomerId = payment.CustomerId,
            Amount = command.RefundAmount,
            Currency = payment.Currency,
            PaymentType = PaymentType.Refund,
            Status = PaymentStatus.Completed,
            PaymentMethod = payment.PaymentMethod,
            Notes = $"Refund for payment {payment.Id}: {command.Reason}",
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _context.Payments.Add(refundRecord);
        await _context.SaveChangesAsync(cancellationToken);

        return MapPayment(refundRecord);
    }

    public async Task<PaymentDto> Handle(RecordInStorePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }
        var tenantId = _tenantContext.TenantId;

        if (command.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        var method = Enum.TryParse<PaymentMethod>(command.PaymentMethod, true, out var parsedMethod)
            ? parsedMethod
            : PaymentMethod.Cash;

        var payment = new Payment
        {
            TenantId = tenantId,
            BookingId = command.BookingId,
            CustomerId = command.CustomerId,
            Amount = command.Amount,
            Currency = "USD",
            PaymentType = PaymentType.InStorePOS,
            Status = PaymentStatus.Completed,
            PaymentMethod = method,
            Notes = command.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        // Update customer total spent amount if customerId provided
        if (command.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == command.CustomerId.Value && c.TenantId == tenantId, cancellationToken);
            if (customer != null)
            {
                customer.TotalSpentAmount += command.Amount;
                customer.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapPayment(payment);
    }

    private static PaymentDto MapPayment(Payment p) => new(
        p.Id,
        p.TenantId,
        p.BookingId,
        p.CustomerId,
        null,
        p.Amount,
        p.Currency,
        p.PaymentType.ToString(),
        p.Status.ToString(),
        p.PaymentMethod.ToString(),
        p.StripePaymentIntentId,
        p.StripeCheckoutSessionId,
        p.ReceiptUrl,
        p.Notes,
        p.CreatedAtUtc,
        p.CompletedAtUtc
    );
}
