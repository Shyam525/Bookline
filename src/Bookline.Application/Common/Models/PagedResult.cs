namespace Bookline.Application.Common.Models;

public record PagedQuery(int Page = 1, int PageSize = 10)
{
    public int Page { get; init; } = Page <= 0 ? 1 : Page;
    public int PageSize { get; init; } = PageSize <= 0 ? 10 : (PageSize > 100 ? 100 : PageSize);
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
