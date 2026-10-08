using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IPaymentProvider _paymentProvider;

    public OrdersController(
        BooklineDbContext dbContext,
        ITenantContext tenantContext,
        IPaymentProvider paymentProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _paymentProvider = paymentProvider;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { Message = "Order must have at least one product." });
        }

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return BadRequest(new { Message = "Provider not found." });
        }

        // Fetch products and validate stock
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _dbContext.Products.IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id) && p.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        decimal subtotal = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var itemReq in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == itemReq.ProductId);
            if (product == null)
            {
                return BadRequest(new { Message = $"Product {itemReq.ProductId} not found." });
            }

            if (!product.Purchase(itemReq.Quantity))
            {
                return BadRequest(new { Message = $"Insufficient inventory for product: {product.Name}." });
            }

            var lineTotal = product.Price * itemReq.Quantity;
            subtotal += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = itemReq.Quantity,
                TotalPrice = lineTotal
            });
        }

        var tax = Math.Round(subtotal * 0.05m, 2);
        var totalAmount = subtotal + tax;

        // Resolve customer id from auth or request
        Guid customerId = Guid.NewGuid();
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            customerId = parsedUserId;
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CustomerId = customerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            Subtotal = subtotal,
            Tax = tax,
            TotalAmount = totalAmount,
            Currency = tenant.Currency,
            Status = OrderStatus.Paid, // Completed instant payment in checkout
            PaidAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Items = orderItems
        };

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
        }

        _dbContext.Orders.Add(order);

        // Process payment and calculate platform commission (Point 52, 55)
        var paymentResult = await _paymentProvider.ProcessPaymentAsync(new ProcessPaymentRequest(
            TenantId: tenant.Id,
            CustomerId: customerId,
            BookingId: null,
            OrderId: order.Id,
            Amount: totalAmount,
            Currency: tenant.Currency,
            PaymentMethod: "CreditCard",
            Notes: $"Order checkout: {order.OrderNumber}"
        ), cancellationToken);

        if (paymentResult.Success)
        {
            order.PaymentId = paymentResult.PaymentId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Order = order,
            Payment = paymentResult
        });
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetCustomerOrders(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var customerId))
        {
            return Unauthorized();
        }

        var orders = await _dbContext.Orders.IgnoreQueryFilters()
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("provider")]
    [Authorize]
    public async Task<IActionResult> GetProviderOrders(CancellationToken cancellationToken)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null) return NotFound(new { Message = "Order not found." });
        return Ok(order);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null) return NotFound(new { Message = "Order not found." });

        if (Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            order.Status = newStatus;
            order.UpdatedAtUtc = DateTime.UtcNow;
            if (newStatus == OrderStatus.Completed) order.CompletedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(order);
        }

        return BadRequest(new { Message = "Invalid order status." });
    }
}

public record OrderItemRequest(Guid ProductId, int Quantity);
public record CreateOrderRequest(
    Guid TenantId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? ShippingAddress,
    string? Notes,
    List<OrderItemRequest> Items
);
public record UpdateOrderStatusRequest(string Status);
