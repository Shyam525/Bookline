using Bookline.Api.Extensions;
using Bookline.Api.Validators;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Common;
using Bookline.Domain.Entities;
using Bookline.Domain.Enums;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IIdempotencyService _idempotencyService;

    public OrdersController(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        IPaymentProvider paymentProvider,
        IIdempotencyService idempotencyService)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _paymentProvider = paymentProvider;
        _idempotencyService = idempotencyService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var idempKey = Request?.Headers?["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            var cached = await _idempotencyService.GetExistingAsync(idempKey, "CreateOrder", cancellationToken);
            if (cached != null)
            {
                var cachedObj = System.Text.Json.JsonSerializer.Deserialize<object>(cached.ResponseJson);
                return StatusCode(cached.StatusCode, cachedObj);
            }
        }

        var validator = new CreateOrderRequestValidator();
        var valResult = await validator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            return this.ApiValidation(errors);
        }

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            return this.ApiNotFound("Provider not found.");
        }

        // Fetch products and validate stock at authoritative mutation boundary
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _dbContext.Products.IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id) && p.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        decimal subtotal = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var itemReq in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == itemReq.ProductId);
            if (product == null || !product.IsActive)
            {
                return this.ApiNotFound($"Product {itemReq.ProductId} not found or inactive.");
            }

            if (!product.Purchase(itemReq.Quantity))
            {
                return this.ApiConflict("INSUFFICIENT_INVENTORY", $"Insufficient inventory for product: {product.Name}.");
            }

            var unitPriceMoney = new Money(product.Price, tenant.Currency);
            var lineTotalMoney = unitPriceMoney * itemReq.Quantity;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = unitPriceMoney.Amount,
                Quantity = itemReq.Quantity,
                TotalPrice = lineTotalMoney.Amount,
                Currency = tenant.Currency
            });
        }

        var orderSummary = FinancialReconciliation.CalculateOrderSummary(
            orderItems.Select(i => new Money(i.TotalPrice, i.Currency)),
            tenant.Currency,
            taxRatePercentage: 5.00m);

        subtotal = orderSummary.Subtotal.Amount;
        var tax = orderSummary.Tax.Amount;
        var totalAmount = orderSummary.TotalAmount.Amount;

        // Resolve customer id from auth or request
        Guid customerId = Guid.NewGuid();
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            customerId = parsedUserId;
        }

        // Resolve optional related booking (Section 27: Services + Products Combined Flow)
        Booking? relatedBooking = null;
        if (request.RelatedBookingId.HasValue)
        {
            relatedBooking = await _dbContext.Bookings.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == request.RelatedBookingId.Value && b.TenantId == request.TenantId, cancellationToken);
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
            RelatedBookingId = relatedBooking?.Id,
            RelatedBookingReference = relatedBooking?.BookingReference,
            Items = orderItems
        };

        if (relatedBooking != null)
        {
            relatedBooking.AttachRelatedOrder(order.Id, order.OrderNumber);
        }

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
        }

        _dbContext.Orders.Add(order);

        // Process payment and calculate platform commission (Sections 85, 88)
        var paymentResult = await _paymentProvider.ProcessPaymentAsync(new ProcessPaymentRequest(
            TenantId: tenant.Id,
            CustomerId: customerId,
            BookingId: null,
            OrderId: order.Id,
            Amount: totalAmount,
            Currency: tenant.Currency,
            PaymentMethod: request.PaymentMethod ?? "CreditCard",
            Notes: $"Order checkout: {order.OrderNumber}"
        ), cancellationToken);

        if (paymentResult.Success)
        {
            order.PaymentId = paymentResult.PaymentId;
            order.Status = OrderStatus.Paid;

            // Section 90 & 91: Transactional Outbox + Audit
            _dbContext.AuditLogs.Add(new AuditLog
            {
                TenantId = tenant.Id,
                Actor = customerId.ToString(),
                Action = "Order.Created",
                Target = order.Id.ToString(),
                MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    PaymentId = paymentResult.PaymentId,
                    ItemCount = orderItems.Count
                })
            });

            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                TenantId = tenant.Id,
                EventType = Bookline.Domain.Constants.NotificationEvents.OrderCreated,
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    TenantId = tenant.Id,
                    CustomerId = order.CustomerId,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    TotalAmount = order.TotalAmount,
                    Currency = order.Currency
                })
            });
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                TenantId = tenant.Id,
                EventType = Bookline.Domain.Constants.NotificationEvents.PaymentFailed,
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OrderId = order.Id,
                    TenantId = tenant.Id,
                    CustomerId = customerId,
                    Error = paymentResult.ErrorMessage
                })
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var responsePayload = new
        {
            Order = new
            {
                order.Id,
                order.OrderNumber,
                order.TenantId,
                order.CustomerId,
                Status = order.Status.ToString(),
                order.Subtotal,
                order.Tax,
                order.TotalAmount,
                order.PaymentId,
                order.RelatedBookingId,
                order.RelatedBookingReference,
                Items = orderItems.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.TotalPrice
                }).ToList(),
                order.CreatedAtUtc
            },
            Payment = paymentResult
        };

        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotencyService.SaveAsync(
                idempKey,
                "CreateOrder",
                200,
                System.Text.Json.JsonSerializer.Serialize(responsePayload),
                tenant.Id,
                null,
                cancellationToken);
        }

        return Ok(responsePayload);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetCustomerOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = "created",
        [FromQuery] string? direction = "desc",
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var customerId))
        {
            return Unauthorized();
        }

        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Orders.IgnoreQueryFilters()
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        bool isAsc = string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLowerInvariant()) switch
        {
            "total" or "amount" => isAsc ? query.OrderBy(o => o.TotalAmount).ThenBy(o => o.Id) : query.OrderByDescending(o => o.TotalAmount).ThenBy(o => o.Id),
            "status" => isAsc ? query.OrderBy(o => o.Status).ThenBy(o => o.Id) : query.OrderByDescending(o => o.Status).ThenBy(o => o.Id),
            _ => isAsc ? query.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.Id) : query.OrderByDescending(o => o.CreatedAtUtc).ThenBy(o => o.Id)
        };

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.TenantId,
                o.CustomerId,
                Status = o.Status.ToString(),
                o.Subtotal,
                o.Tax,
                o.TotalAmount,
                o.PaymentId,
                o.RelatedBookingId,
                o.RelatedBookingReference,
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.TotalPrice
                }).ToList(),
                o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new Bookline.Application.Common.Models.PagedResult<object>(orders, totalCount, page, pageSize));
    }

    [HttpGet("provider")]
    [Authorize]
    public async Task<IActionResult> GetProviderOrders(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = "created",
        [FromQuery] string? direction = "desc",
        CancellationToken cancellationToken = default)
    {
        // Section 84.4: Authoritative Tenant-scoped isolation
        Guid effectiveTenantId;
        if (_tenantContext.IsResolved)
        {
            if (tenantId.HasValue && tenantId.Value != Guid.Empty && tenantId.Value != _tenantContext.TenantId && !User.IsInRole("Admin") && !User.IsInRole("PlatformAdmin"))
            {
                return this.ApiForbidden("Cannot access orders outside your authorized tenant boundary.");
            }
            effectiveTenantId = _tenantContext.TenantId;
        }
        else if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            effectiveTenantId = tenantId.Value;
        }
        else
        {
            return this.ApiForbidden("Tenant context is required.");
        }

        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Orders.IgnoreQueryFilters()
            .Where(o => o.TenantId == effectiveTenantId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(searchLower) ||
                                     (o.CustomerName != null && o.CustomerName.ToLower().Contains(searchLower)) ||
                                     (o.CustomerEmail != null && o.CustomerEmail.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        bool isAsc = string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLowerInvariant()) switch
        {
            "total" or "amount" => isAsc ? query.OrderBy(o => o.TotalAmount).ThenBy(o => o.Id) : query.OrderByDescending(o => o.TotalAmount).ThenBy(o => o.Id),
            "status" => isAsc ? query.OrderBy(o => o.Status).ThenBy(o => o.Id) : query.OrderByDescending(o => o.Status).ThenBy(o => o.Id),
            _ => isAsc ? query.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.Id) : query.OrderByDescending(o => o.CreatedAtUtc).ThenBy(o => o.Id)
        };

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.TenantId,
                o.CustomerId,
                o.CustomerName,
                o.CustomerEmail,
                Status = o.Status.ToString(),
                o.Subtotal,
                o.Tax,
                o.TotalAmount,
                o.PaymentId,
                o.RelatedBookingId,
                o.RelatedBookingReference,
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.TotalPrice
                }).ToList(),
                o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new Bookline.Application.Common.Models.PagedResult<object>(orders, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null) return NotFound(new { Message = "Order not found." });

        return Ok(new
        {
            order.Id,
            order.OrderNumber,
            order.TenantId,
            order.CustomerId,
            Status = order.Status.ToString(),
            order.Subtotal,
            order.Tax,
            order.TotalAmount,
            order.PaymentId,
            order.RelatedBookingId,
            order.RelatedBookingReference,
            Items = order.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.TotalPrice
            }).ToList(),
            order.CreatedAtUtc
        });
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

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.TenantId,
                Status = order.Status.ToString(),
                order.UpdatedAtUtc
            });
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
    List<OrderItemRequest> Items,
    string? PaymentMethod = "CreditCard",
    Guid? RelatedBookingId = null
);
public record UpdateOrderStatusRequest(string Status);
