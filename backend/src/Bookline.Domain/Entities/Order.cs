using Bookline.Domain.Common;
using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class Order : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public void MarkPaid(Guid? paymentId = null)
    {
        Status = OrderStatus.Paid;
        PaymentId = paymentId ?? PaymentId;
        PaidAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        Status = OrderStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        Status = OrderStatus.Ready;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
