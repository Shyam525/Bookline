using System.Linq.Expressions;

namespace Bookline.Application.Common.Models;

public record PagedQuery(int Page = 1, int PageSize = 20, string? Sort = null, string? Direction = "asc")
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = Page;
    public int PageSize { get; init; } = PageSize <= 0 ? DefaultPageSize : (PageSize > MaxPageSize ? MaxPageSize : PageSize);
    public string? Sort { get; init; } = Sort?.Trim();
    public string? Direction { get; init; } = Direction?.Trim();
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items ?? Array.Empty<T>();
        TotalCount = Math.Max(0, totalCount);
        Page = Math.Max(1, page);
        PageSize = pageSize <= 0 ? PagedQuery.DefaultPageSize : pageSize;
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
        new(items, totalCount, page, pageSize);

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
