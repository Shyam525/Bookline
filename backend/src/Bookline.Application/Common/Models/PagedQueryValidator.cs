using FluentValidation;

namespace Bookline.Application.Common.Models;

/// <summary>
/// Section 84.1: FluentValidation for bounded pagination queries.
/// Validates positive page, bounded pageSize, direction, and sort allowlist.
/// </summary>
public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator(int maxPageSize = PagedQuery.MaxPageSize, IEnumerable<string>? allowedSortFields = null)
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be a positive integer greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, maxPageSize)
            .WithMessage($"PageSize must be between 1 and {maxPageSize}.");

        RuleFor(x => x.Direction)
            .Must(d => string.IsNullOrEmpty(d) || d.Equals("asc", StringComparison.OrdinalIgnoreCase) || d.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'asc' or 'desc'.");

        if (allowedSortFields != null)
        {
            var allowedSet = new HashSet<string>(allowedSortFields, StringComparer.OrdinalIgnoreCase);
            RuleFor(x => x.Sort)
                .Must(s => string.IsNullOrEmpty(s) || allowedSet.Contains(s.Trim()))
                .WithMessage($"Sort field is not supported. Allowed fields: {string.Join(", ", allowedSet)}.");
        }
    }
}
