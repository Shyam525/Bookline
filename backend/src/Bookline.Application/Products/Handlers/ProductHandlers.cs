using System.Text.RegularExpressions;
using Bookline.Application.Common.Exceptions;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Products.Commands;
using Bookline.Application.Products.DTOs;
using Bookline.Application.Products.Queries;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bookline.Application.Products.Handlers;

public class ProductHandlers :
    IRequestHandler<GetProductCategoriesQuery, List<ProductCategoryDto>>,
    IRequestHandler<CreateProductCategoryCommand, ProductCategoryDto>,
    IRequestHandler<UpdateProductCategoryCommand, ProductCategoryDto>,
    IRequestHandler<DeleteProductCategoryCommand, Unit>,
    IRequestHandler<GetProductsQuery, List<ProductDto>>,
    IRequestHandler<GetProductByIdQuery, ProductDto>,
    IRequestHandler<GetProductBySlugQuery, ProductDto>,
    IRequestHandler<CreateProductCommand, ProductDto>,
    IRequestHandler<UpdateProductCommand, ProductDto>,
    IRequestHandler<DeleteProductCommand, Unit>,
    IRequestHandler<AdjustInventoryCommand, ProductDto>,
    IRequestHandler<SetProductStatusCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IProductImageService _imageService;

    public ProductHandlers(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IProductImageService imageService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _imageService = imageService;
    }

    // --- Product Categories ---

    public async Task<List<ProductCategoryDto>> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.ProductCategories
            .AsNoTracking()
            .Include(c => c.Products)
            .Where(c => c.TenantId == _tenantContext.TenantId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapCategoryToDto).ToList();
    }

    public async Task<ProductCategoryDto> Handle(CreateProductCategoryCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : Slugify(req.Slug);
        var conflict = await _context.ProductCategories.AnyAsync(
            c => c.TenantId == _tenantContext.TenantId && c.Slug == slug,
            cancellationToken
        );
        if (conflict)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString("n")[..4]}";
        }

        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description,
            SortOrder = req.SortOrder,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return MapCategoryToDto(category);
    }

    public async Task<ProductCategoryDto> Handle(UpdateProductCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == _tenantContext.TenantId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("ProductCategory", command.Id);
        }

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        category.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            category.Slug = Slugify(req.Slug);
        }
        category.Description = req.Description;
        category.SortOrder = req.SortOrder;
        category.IsActive = req.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapCategoryToDto(category);
    }

    public async Task<Unit> Handle(DeleteProductCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _context.ProductCategories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == _tenantContext.TenantId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("ProductCategory", command.Id);
        }

        if (category.Products != null && category.Products.Any(p => p.IsActive))
        {
            throw new ValidationException("Cannot delete category because active products are assigned to it.");
        }

        category.IsActive = false;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    // --- Products ---

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.TenantId == _tenantContext.TenantId);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Sku != null && p.Sku.ToLower().Contains(search)) ||
                p.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var normalizedStatus = request.Status.Trim().ToLowerInvariant();
            query = normalizedStatus switch
            {
                "active_in_stock" or "in_stock" => query.Where(p => p.IsActive && p.IsPublished && (!p.TrackInventory || p.StockQuantity - p.ReservedQuantity > 0) && p.IsPurchasableOnline),
                "out_of_stock" => query.Where(p => p.IsActive && p.IsPublished && p.TrackInventory && p.StockQuantity - p.ReservedQuantity <= 0),
                "low_stock" => query.Where(p => p.IsActive && p.TrackInventory && (p.StockQuantity - p.ReservedQuantity > 0) && (p.StockQuantity - p.ReservedQuantity <= p.LowStockThreshold)),
                "store_only" => query.Where(p => p.IsActive && p.IsPublished && !p.IsPurchasableOnline),
                "draft" or "unpublished" => query.Where(p => p.IsActive && !p.IsPublished),
                "inactive" => query.Where(p => !p.IsActive),
                "all" => query,
                _ => request.IncludeInactive ? query : query.Where(p => p.IsActive)
            };
        }
        else if (!request.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return products.Select(MapProductToDto).ToList();
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        return MapProductToDto(product);
    }

    public async Task<ProductDto> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Slug);
        }

        return MapProductToDto(product);
    }

    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        ValidateProductInput(req.Name, req.Price, req.Currency, req.StockQuantity, req.LowStockThreshold);

        if (req.CategoryId.HasValue)
        {
            var categoryExists = await _context.ProductCategories.AnyAsync(
                c => c.Id == req.CategoryId.Value && c.TenantId == _tenantContext.TenantId,
                cancellationToken
            );
            if (!categoryExists)
            {
                throw new NotFoundException("ProductCategory", req.CategoryId.Value);
            }
        }

        // SKU uniqueness validation within the business
        if (!string.IsNullOrWhiteSpace(req.Sku))
        {
            var normalizedSku = req.Sku.Trim().ToLowerInvariant();
            var skuExists = await _context.Products.AnyAsync(
                p => p.TenantId == _tenantContext.TenantId && p.IsActive && p.Sku != null && p.Sku.ToLower() == normalizedSku,
                cancellationToken
            );
            if (skuExists)
            {
                throw new ValidationException($"A product with SKU '{req.Sku}' already exists for this business.");
            }
        }

        // Slug generation and uniqueness
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : Slugify(req.Slug);
        var slugConflict = await _context.Products.AnyAsync(
            p => p.TenantId == _tenantContext.TenantId && p.Slug == slug,
            cancellationToken
        );
        if (slugConflict)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString("n")[..4]}";
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = req.CategoryId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description?.Trim() ?? string.Empty,
            Price = req.Price,
            Currency = (req.Currency ?? "USD").Trim().ToUpperInvariant(),
            Sku = string.IsNullOrWhiteSpace(req.Sku) ? null : req.Sku.Trim(),
            TrackInventory = req.TrackInventory,
            StockQuantity = req.StockQuantity,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            LowStockThreshold = req.LowStockThreshold,
            IsPurchasableOnline = req.IsPurchasableOnline,
            IsPublished = req.IsPublished,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Process images
        await ProcessProductImagesAsync(product, req.Images, req.ImageUrl, cancellationToken);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Load category navigation if attached
        if (product.CategoryId.HasValue)
        {
            product.Category = await _context.ProductCategories.FindAsync(new object[] { product.CategoryId.Value }, cancellationToken);
        }

        return MapProductToDto(product);
    }

    public async Task<ProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        var req = command.Request;
        ValidateProductInput(req.Name, req.Price, req.Currency, req.StockQuantity, req.LowStockThreshold);

        if (req.CategoryId.HasValue && req.CategoryId.Value != product.CategoryId)
        {
            var categoryExists = await _context.ProductCategories.AnyAsync(
                c => c.Id == req.CategoryId.Value && c.TenantId == _tenantContext.TenantId,
                cancellationToken
            );
            if (!categoryExists)
            {
                throw new NotFoundException("ProductCategory", req.CategoryId.Value);
            }
        }

        // SKU uniqueness validation
        if (!string.IsNullOrWhiteSpace(req.Sku))
        {
            var normalizedSku = req.Sku.Trim().ToLowerInvariant();
            var skuConflict = await _context.Products.AnyAsync(
                p => p.TenantId == _tenantContext.TenantId && p.Id != product.Id && p.IsActive && p.Sku != null && p.Sku.ToLower() == normalizedSku,
                cancellationToken
            );
            if (skuConflict)
            {
                throw new ValidationException($"A product with SKU '{req.Sku}' already exists for this business.");
            }
        }

        // Slug check
        if (!string.IsNullOrWhiteSpace(req.Slug) && req.Slug != product.Slug)
        {
            var newSlug = Slugify(req.Slug);
            var slugConflict = await _context.Products.AnyAsync(
                p => p.TenantId == _tenantContext.TenantId && p.Id != product.Id && p.Slug == newSlug,
                cancellationToken
            );
            if (slugConflict)
            {
                newSlug = $"{newSlug}-{Guid.NewGuid().ToString("n")[..4]}";
            }
            product.Slug = newSlug;
        }

        product.Name = req.Name.Trim();
        product.Description = req.Description?.Trim() ?? string.Empty;
        product.Price = req.Price;
        product.Currency = (req.Currency ?? product.Currency).Trim().ToUpperInvariant();
        product.Sku = string.IsNullOrWhiteSpace(req.Sku) ? null : req.Sku.Trim();
        product.CategoryId = req.CategoryId;
        product.TrackInventory = req.TrackInventory;
        product.StockQuantity = req.StockQuantity;
        product.LowStockThreshold = req.LowStockThreshold;
        product.IsPurchasableOnline = req.IsPurchasableOnline;
        product.IsPublished = req.IsPublished;
        product.IsActive = req.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        // Process images update
        _context.ProductImages.RemoveRange(product.Images);
        product.Images.Clear();
        await ProcessProductImagesAsync(product, req.Images, req.ImageUrl, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (product.CategoryId.HasValue)
        {
            product.Category = await _context.ProductCategories.FindAsync(new object[] { product.CategoryId.Value }, cancellationToken);
        }

        return MapProductToDto(product);
    }

    public async Task<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        product.IsActive = false;
        product.IsPublished = false;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<ProductDto> Handle(AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        if (command.Request.NewStockQuantity < 0)
        {
            throw new ValidationException("Inventory stock level cannot be negative.");
        }

        if (!product.AdjustStock(command.Request.NewStockQuantity))
        {
            throw new ValidationException("Failed to adjust inventory stock.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapProductToDto(product);
    }

    public async Task<ProductDto> Handle(SetProductStatusCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.TenantId == _tenantContext.TenantId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        var req = command.Request;
        if (req.IsActive.HasValue) product.IsActive = req.IsActive.Value;
        if (req.IsPublished.HasValue) product.IsPublished = req.IsPublished.Value;
        if (req.IsPurchasableOnline.HasValue) product.IsPurchasableOnline = req.IsPurchasableOnline.Value;

        product.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapProductToDto(product);
    }

    // --- Helper Methods ---

    private async Task ProcessProductImagesAsync(
        Product product,
        List<ProductImageInput>? imageInputs,
        string? singleImageUrl,
        CancellationToken cancellationToken)
    {
        var imagesToAdd = new List<ProductImage>();

        if (imageInputs != null && imageInputs.Count > 0)
        {
            int order = 0;
            bool primarySet = false;

            foreach (var input in imageInputs)
            {
                if (string.IsNullOrWhiteSpace(input.Url)) continue;

                var validation = await _imageService.ValidateImageUrlAsync(input.Url, cancellationToken);
                if (!validation.IsValid)
                {
                    throw new ValidationException(validation.ErrorMessage ?? "Invalid product image URL.");
                }

                var isPrimary = input.IsPrimary || (!primarySet && order == 0);
                if (isPrimary) primarySet = true;

                imagesToAdd.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId,
                    ProductId = product.Id,
                    Url = validation.NormalizedUrl ?? input.Url.Trim(),
                    AltText = input.AltText,
                    SortOrder = input.SortOrder > 0 ? input.SortOrder : order++,
                    IsPrimary = isPrimary,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(singleImageUrl))
        {
            var validation = await _imageService.ValidateImageUrlAsync(singleImageUrl, cancellationToken);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.ErrorMessage ?? "Invalid product image URL.");
            }

            imagesToAdd.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ProductId = product.Id,
                Url = validation.NormalizedUrl ?? singleImageUrl.Trim(),
                AltText = product.Name,
                SortOrder = 0,
                IsPrimary = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        foreach (var img in imagesToAdd)
        {
            product.Images.Add(img);
        }

        var primary = imagesToAdd.FirstOrDefault(i => i.IsPrimary) ?? imagesToAdd.FirstOrDefault();
        product.ImageUrl = primary?.Url;
    }

    private static void ValidateProductInput(string name, decimal price, string? currency, int stockQuantity, int lowStockThreshold)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Product name is required.");

        if (price < 0)
            throw new ValidationException("Product price must be non-negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException("Currency is required.");

        if (currency.Trim().Length != 3)
            throw new ValidationException("Currency must be a valid 3-letter currency code (e.g. USD).");

        if (stockQuantity < 0)
            throw new ValidationException("Inventory stock quantity cannot be negative.");

        if (lowStockThreshold < 0)
            throw new ValidationException("Low stock threshold cannot be negative.");
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("n")[..8];
        var slug = text.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }

    private static ProductCategoryDto MapCategoryToDto(ProductCategory category)
    {
        return new ProductCategoryDto(
            category.Id,
            category.TenantId,
            category.Name,
            category.Slug,
            category.Description,
            category.SortOrder,
            category.IsActive,
            category.Products?.Count(p => p.IsActive) ?? 0
        );
    }

    private static ProductDto MapProductToDto(Product product)
    {
        var inventoryStatus = !product.TrackInventory
            ? "Untracked"
            : product.AvailableQuantity <= 0
                ? "OutOfStock"
                : product.IsLowStock
                    ? "LowStock"
                    : "InStock";

        var commercialStatus = !product.IsActive
            ? "Inactive"
            : !product.IsPublished
                ? "Draft"
                : !product.IsPurchasableOnline
                    ? "StoreOnly"
                    : product.IsInStock
                        ? "ActiveInStock"
                        : "ActiveOutOfStock";

        var images = product.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(
                i.Id,
                i.ProductId,
                i.Url,
                i.AltText,
                i.SortOrder,
                i.IsPrimary,
                i.CreatedAtUtc
            ))
            .ToList();

        return new ProductDto(
            product.Id,
            product.TenantId,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.Currency,
            product.Sku,
            product.CategoryId,
            product.Category?.Name,
            images,
            product.ImageUrl,
            product.TrackInventory,
            product.StockQuantity,
            product.ReservedQuantity,
            product.SoldQuantity,
            product.AvailableQuantity,
            product.LowStockThreshold,
            product.AllowBackorders,
            product.IsInStock,
            product.IsLowStock,
            product.IsActive,
            product.IsPurchasableOnline,
            product.IsPublished,
            inventoryStatus,
            commercialStatus,
            product.CreatedAtUtc,
            product.UpdatedAtUtc
        );
    }
}
