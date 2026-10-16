using Bookline.Application.Products.DTOs;
using MediatR;

namespace Bookline.Application.Products.Queries;

public record GetProductCategoriesQuery : IRequest<List<ProductCategoryDto>>;

public record GetProductsQuery(
    Guid? CategoryId = null,
    string? Status = null,
    bool IncludeInactive = false,
    string? Search = null
) : IRequest<List<ProductDto>>;

public record GetPagedProductsQuery(
    Guid? CategoryId = null,
    string? Status = null,
    bool IncludeInactive = false,
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? Sort = "name",
    string? Direction = "asc"
) : IRequest<Bookline.Application.Common.Models.PagedResult<ProductDto>>;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto>;
