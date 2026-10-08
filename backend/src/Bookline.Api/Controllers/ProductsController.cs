using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly BooklineDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ProductsController(BooklineDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null) return NotFound(new { Message = "Product not found." });
        return Ok(product);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { Message = "Product name is required." });
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            Currency = request.Currency ?? "USD",
            Sku = request.Sku,
            ImageUrl = request.ImageUrl,
            StockQuantity = request.StockQuantity,
            IsActive = true,
            IsPurchasableOnline = request.IsPurchasableOnline,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null) return NotFound(new { Message = "Product not found." });

        product.Name = request.Name;
        product.Description = request.Description ?? string.Empty;
        product.Price = request.Price;
        product.Currency = request.Currency ?? product.Currency;
        product.Sku = request.Sku;
        product.ImageUrl = request.ImageUrl;
        product.StockQuantity = request.StockQuantity;
        product.IsPurchasableOnline = request.IsPurchasableOnline;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null) return NotFound(new { Message = "Product not found." });

        product.IsActive = false;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string? Currency,
    string? Sku,
    string? ImageUrl,
    int StockQuantity,
    bool IsPurchasableOnline,
    Guid? CategoryId
);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string? Currency,
    string? Sku,
    string? ImageUrl,
    int StockQuantity,
    bool IsPurchasableOnline,
    bool IsActive
);
