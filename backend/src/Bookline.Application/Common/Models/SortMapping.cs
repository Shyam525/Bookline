using System.Linq.Expressions;

namespace Bookline.Application.Common.Models;

/// <summary>
/// Section 84: Safe allowlist mapping of API sort fields to strongly typed expressions
/// with deterministic tie-breaking. Untrusted user strings are never directly interpolated into queries.
/// </summary>
public class SortMapping<TEntity>
{
    private readonly Dictionary<string, (Expression<Func<TEntity, object>> KeySelector, bool DefaultDescending)> _mappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Expression<Func<TEntity, object>> _tieBreaker;
    private readonly bool _tieBreakerDescending;

    public SortMapping(Expression<Func<TEntity, object>> tieBreaker, bool tieBreakerDescending = false)
    {
        _tieBreaker = tieBreaker ?? throw new ArgumentNullException(nameof(tieBreaker));
        _tieBreakerDescending = tieBreakerDescending;
    }

    public SortMapping<TEntity> Add(string fieldName, Expression<Func<TEntity, object>> keySelector, bool defaultDescending = false)
    {
        _mappings[fieldName] = (keySelector, defaultDescending);
        return this;
    }

    public IReadOnlyCollection<string> AllowedFields => _mappings.Keys;

    public bool IsAllowed(string? fieldName) =>
        string.IsNullOrWhiteSpace(fieldName) || _mappings.ContainsKey(fieldName.Trim());

    public IQueryable<TEntity> Apply(
        IQueryable<TEntity> query,
        string? sortField,
        string? direction = null,
        string? defaultField = null)
    {
        var field = string.IsNullOrWhiteSpace(sortField) ? defaultField : sortField.Trim();
        bool isDescending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(field) && _mappings.TryGetValue(field, out var mapping))
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                isDescending = mapping.DefaultDescending;
            }

            query = isDescending
                ? query.OrderByDescending(mapping.KeySelector)
                : query.OrderBy(mapping.KeySelector);
        }

        // Section 84.3: Add stable tie-breaker to prevent equal sort values from drifting between pages
        if (query is IOrderedQueryable<TEntity> orderedQuery)
        {
            return _tieBreakerDescending
                ? orderedQuery.ThenByDescending(_tieBreaker)
                : orderedQuery.ThenBy(_tieBreaker);
        }

        return _tieBreakerDescending
            ? query.OrderByDescending(_tieBreaker)
            : query.OrderBy(_tieBreaker);
    }
}
