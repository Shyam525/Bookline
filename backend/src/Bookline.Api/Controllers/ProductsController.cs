using Bookline.Application.Products.Commands;
using Bookline.Application.Products.DTOs;
using Bookline.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Route("api/provider/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists retail products for the authorized business with filtering by category, status, and search.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null)
    {
        var result = await _sender.Send(new GetProductsQuery(categoryId, status, includeInactive, search));
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single retail product by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        var result = await _sender.Send(new GetProductByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a retail product by public slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductDto>> GetProductBySlug(string slug)
    {
        var result = await _sender.Send(new GetProductBySlugQuery(slug));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new retail product with SKU uniqueness, pricing, and inventory validation.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var result = await _sender.Send(new CreateProductCommand(request));
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing retail product under the authorized business.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var result = await _sender.Send(new UpdateProductCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Soft deletes (deactivates) a retail product.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await _sender.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Adjusts inventory stock count with audit reason and non-negative stock validation.
    /// </summary>
    [HttpPatch("{id:guid}/inventory")]
    public async Task<ActionResult<ProductDto>> AdjustInventory(Guid id, [FromBody] AdjustInventoryRequest request)
    {
        var result = await _sender.Send(new AdjustInventoryCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Updates product operational status (active, published, purchasable online).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ProductDto>> SetStatus(Guid id, [FromBody] SetProductStatusRequest request)
    {
        var result = await _sender.Send(new SetProductStatusCommand(id, request));
        return Ok(result);
    }

    // --- Product Categories ---

    /// <summary>
    /// Lists all product categories for the authorized business.
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<ProductCategoryDto>>> GetCategories()
    {
        var result = await _sender.Send(new GetProductCategoriesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product category for retail merchandise.
    /// </summary>
    [HttpPost("categories")]
    public async Task<ActionResult<ProductCategoryDto>> CreateCategory([FromBody] CreateProductCategoryRequest request)
    {
        var result = await _sender.Send(new CreateProductCategoryCommand(request));
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing product category.
    /// </summary>
    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<ProductCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateProductCategoryRequest request)
    {
        var result = await _sender.Send(new UpdateProductCategoryCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Deletes a product category if no active products are assigned.
    /// </summary>
    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        await _sender.Send(new DeleteProductCategoryCommand(id));
        return NoContent();
    }
}
