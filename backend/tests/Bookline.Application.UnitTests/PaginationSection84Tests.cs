using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
using Bookline.Domain.Entities;
using FluentValidation.TestHelper;
using Xunit;

namespace Bookline.Application.UnitTests;

public class PaginationSection84Tests
{
    private class TestItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public void PagedQuery_EnforcesDefaultAndMaxBounds_WhenInputIsOutOfRange()
    {
        // Default constructor should supply standard 1 and 20
        var qDefault = new PagedQuery();
        Assert.Equal(1, qDefault.Page);
        Assert.Equal(PagedQuery.DefaultPageSize, qDefault.PageSize);

        // Zero or negative pageSize should default to DefaultPageSize (20)
        var q3 = new PagedQuery(Page: 1, PageSize: 0);
        Assert.Equal(PagedQuery.DefaultPageSize, q3.PageSize);

        var q4 = new PagedQuery(Page: 1, PageSize: -10);
        Assert.Equal(PagedQuery.DefaultPageSize, q4.PageSize);

        // PageSize > MaxPageSize (100) must be capped at 100
        var q5 = new PagedQuery(Page: 1, PageSize: 500);
        Assert.Equal(PagedQuery.MaxPageSize, q5.PageSize);
    }

    [Fact]
    public void PagedResult_CalculatesTotalPagesCorrectly_IncludingZeroCountAndBoundaryDivision()
    {
        // 0 items => 0 pages
        var res0 = new PagedResult<string>(Array.Empty<string>(), 0, 1, 20);
        Assert.Equal(0, res0.TotalPages);
        Assert.Empty(res0.Items);

        // Exact multiple: 40 items with pageSize 20 => 2 pages
        var resExact = new PagedResult<int>(new[] { 1, 2 }, 40, 1, 20);
        Assert.Equal(2, resExact.TotalPages);

        // Remainder: 41 items with pageSize 20 => 3 pages
        var resRemainder = new PagedResult<int>(new[] { 1, 2 }, 41, 1, 20);
        Assert.Equal(3, resRemainder.TotalPages);

        // 1 item with pageSize 10 => 1 page
        var res1 = new PagedResult<int>(new[] { 1 }, 1, 1, 10);
        Assert.Equal(1, res1.TotalPages);

        // Safe division when pageSize is <= 0
        var resZero = new PagedResult<int>(Array.Empty<int>(), 10, 1, 0);
        Assert.True(resZero.TotalPages >= 0);
    }

    [Fact]
    public void PagedQueryValidator_ValidatesPositivePageAndBoundedPageSize()
    {
        var validator = new PagedQueryValidator(maxPageSize: 50);

        // Valid query
        var validQuery = new PagedQuery(Page: 1, PageSize: 25, Direction: "asc");
        var validResult = validator.TestValidate(validQuery);
        validResult.ShouldNotHaveAnyValidationErrors();

        // Invalid page
        var invalidPageQuery = new PagedQuery { Page = 0 };
        var pageResult = validator.TestValidate(invalidPageQuery);
        pageResult.ShouldHaveValidationErrorFor(x => x.Page);

        // Invalid direction
        var invalidDirQuery = new PagedQuery(Direction: "sideways");
        var dirResult = validator.TestValidate(invalidDirQuery);
        dirResult.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void PagedQueryValidator_EnforcesSortAllowlist_RejectsUnauthorizedSortFields()
    {
        var allowed = new[] { "name", "created", "price" };
        var validator = new PagedQueryValidator(maxPageSize: 100, allowedSortFields: allowed);

        // Valid sort
        var valid = new PagedQuery(Sort: "name");
        validator.TestValidate(valid).ShouldNotHaveValidationErrorFor(x => x.Sort);

        // Disallowed sort field (SQL injection attempt or unknown column)
        var malicious = new PagedQuery(Sort: "name; DROP TABLE Users; --");
        var result = validator.TestValidate(malicious);
        result.ShouldHaveValidationErrorFor(x => x.Sort);
    }

    [Fact]
    public void SortMapping_AppliesAscendingAndDescending_WithDeterministicTieBreaker()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var id3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var data = new List<TestItem>
        {
            new() { Id = id2, Name = "Alpha", Price = 50m, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = id1, Name = "Alpha", Price = 100m, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = id3, Name = "Beta", Price = 25m, CreatedAt = DateTime.UtcNow }
        }.AsQueryable();

        var sortMapping = new SortMapping<TestItem>(x => x.Id)
            .Add("name", x => x.Name)
            .Add("price", x => x.Price)
            .Add("created", x => x.CreatedAt, defaultDescending: true);

        // Sort by Name asc: items with same name ("Alpha") must be tie-broken by Id asc (id1 then id2)
        var sortedByName = sortMapping.Apply(data, "name", "asc").ToList();
        Assert.Equal(id1, sortedByName[0].Id);
        Assert.Equal(id2, sortedByName[1].Id);
        Assert.Equal(id3, sortedByName[2].Id);

        // Sort by Price desc: 100m, 50m, 25m
        var sortedByPrice = sortMapping.Apply(data, "price", "desc").ToList();
        Assert.Equal(100m, sortedByPrice[0].Price);
        Assert.Equal(50m, sortedByPrice[1].Price);
        Assert.Equal(25m, sortedByPrice[2].Price);
    }

    [Fact]
    public void SortMapping_FallsBackToDefault_WhenSortFieldIsUnknownOrEmpty()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var data = new List<TestItem>
        {
            new() { Id = idB, Name = "Zebra" },
            new() { Id = idA, Name = "Apple" }
        }.AsQueryable();

        var sortMapping = new SortMapping<TestItem>(x => x.Id)
            .Add("name", x => x.Name);

        // Empty sort field with defaultField = "name"
        var result = sortMapping.Apply(data, sortField: null, direction: "asc", defaultField: "name").ToList();
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    [Fact]
    public void GetCustomersQueryValidator_ValidatesSearchLengthAndSortAllowlist()
    {
        var validator = new GetCustomersQueryValidator();

        // Valid
        var valid = new GetCustomersQuery(Page: 1, PageSize: 20, Sort: "email", Direction: "asc");
        validator.TestValidate(valid).ShouldNotHaveAnyValidationErrors();

        // Excessive search string
        var longSearch = new GetCustomersQuery(SearchQuery: new string('x', 101));
        validator.TestValidate(longSearch).ShouldHaveValidationErrorFor(x => x.SearchQuery);

        // Disallowed sort field
        var invalidSort = new GetCustomersQuery(Sort: "password_hash");
        validator.TestValidate(invalidSort).ShouldHaveValidationErrorFor(x => x.Sort);
    }
}
