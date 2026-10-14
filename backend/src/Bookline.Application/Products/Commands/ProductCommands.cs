using Bookline.Application.Products.DTOs;
using MediatR;

namespace Bookline.Application.Products.Commands;

public record CreateProductCategoryCommand(CreateProductCategoryRequest Request) : IRequest<ProductCategoryDto>;

public record UpdateProductCategoryCommand(Guid Id, UpdateProductCategoryRequest Request) : IRequest<ProductCategoryDto>;

public record DeleteProductCategoryCommand(Guid Id) : IRequest<Unit>;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductDto>;

public record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest<ProductDto>;

public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public record AdjustInventoryCommand(Guid Id, AdjustInventoryRequest Request) : IRequest<ProductDto>;

public record SetProductStatusCommand(Guid Id, SetProductStatusRequest Request) : IRequest<ProductDto>;
