using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Infrastructure.Payments;

public class PaymentProvider : IPaymentProvider
{
    private readonly BooklineDbContext _dbContext;

    public PaymentProvider(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string ProviderName => "MarketplacePaymentEngine";

    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return new PaymentResult(false, Guid.Empty, string.Empty, "Failed", null, "Tenant not found.");
        }

        var transactionRef = $"TXN-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CustomerId = request.CustomerId,
            BookingId = request.BookingId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentType = request.BookingId.HasValue ? PaymentType.Deposit : PaymentType.FullPayment,
            ReceiptUrl = $"/receipts/{transactionRef}",
            Notes = request.Notes ?? $"Marketplace payment: {transactionRef}",
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _dbContext.Payments.Add(payment);

        // Calculate marketplace platform commission (Point 55)
        var commission = Commission.Calculate(
            tenant.Id,
            request.Amount,
            tenant.CommissionRatePercentage,
            request.Currency,
            payment.Id,
            request.BookingId,
            request.OrderId
        );

        _dbContext.Commissions.Add(commission);

        // Update provider balance (Point 56)
        tenant.PendingPayoutBalance += commission.ProviderNetAmount;
        tenant.AvailablePayoutBalance += commission.ProviderNetAmount;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentResult(
            Success: true,
            PaymentId: payment.Id,
            TransactionReference: transactionRef,
            Status: "Succeeded",
            ReceiptUrl: payment.ReceiptUrl,
            ErrorMessage: null
        );
    }

    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
        {
            return new RefundResult(false, string.Empty, "Failed", "Payment record not found.");
        }

        payment.Status = PaymentStatus.Refunded;

        var commission = await _dbContext.Commissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.PaymentId == payment.Id, cancellationToken);

        if (commission != null)
        {
            commission.Status = "Refunded";
            var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == payment.TenantId, cancellationToken);

            if (tenant != null)
            {
                tenant.AvailablePayoutBalance = Math.Max(0, tenant.AvailablePayoutBalance - commission.ProviderNetAmount);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var refundRef = $"REF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        return new RefundResult(true, refundRef, "Refunded", null);
    }
}

public class PayoutProvider : IPayoutProvider
{
    private readonly BooklineDbContext _dbContext;

    public PayoutProvider(BooklineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PayoutResult> DisbursePayoutAsync(ProcessPayoutRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return new PayoutResult(false, Guid.Empty, string.Empty, "Failed", "Provider not found.");
        }

        if (tenant.AvailablePayoutBalance < request.Amount)
        {
            return new PayoutResult(false, Guid.Empty, string.Empty, "Failed", "Insufficient available balance for payout.");
        }

        var payoutRef = $"PO-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = PayoutStatus.Paid,
            Method = request.Method,
            DestinationAccount = request.DestinationAccount ?? "Primary Connected Account",
            Reference = payoutRef,
            PaidAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        tenant.AvailablePayoutBalance -= request.Amount;
        tenant.PaidOutBalance += request.Amount;

        _dbContext.Payouts.Add(payout);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PayoutResult(true, payout.Id, payoutRef, "Paid", null);
    }
}
