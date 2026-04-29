using FluentAssertions;
using MidgardAddressBook.Core.Models.Pagination;
using Xunit;

namespace MidgardAddressBook.Core.Tests.Models.Pagination;

/// <summary>
/// Tests for <see cref="PagedQuery"/> covering sanitization of all fields:
/// page clamping, page-size clamping, sort-field validation, and search-text normalization.
/// </summary>
public class PagedQueryTests
{
    // ---- Page clamping -------------------------------------------------------

    [Fact]
    public void Should_ClampPage_When_PageIsZero()
    {
        var query = new PagedQuery(
            page: 0,
            pageSize: 25,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.Page.Should().Be(1);
    }

    [Fact]
    public void Should_ClampPage_When_PageIsNegative()
    {
        var query = new PagedQuery(
            page: -5,
            pageSize: 25,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.Page.Should().Be(1);
    }

    [Fact]
    public void Should_PreservePage_When_PageIsValid()
    {
        var query = new PagedQuery(
            page: 3,
            pageSize: 25,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.Page.Should().Be(3);
    }

    // ---- PageSize clamping ---------------------------------------------------

    [Fact]
    public void Should_ClampPageSize_When_PageSizeIsZero()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 0,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.PageSize.Should().Be(1);
    }

    [Fact]
    public void Should_ClampPageSize_When_PageSizeIsNegative()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: -10,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.PageSize.Should().Be(1);
    }

    [Fact]
    public void Should_ClampPageSize_When_PageSizeExceedsMaximum()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 101,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.PageSize.Should().Be(100);
    }

    [Fact]
    public void Should_PreservePageSize_When_PageSizeIsAtMaximum()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 100,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.PageSize.Should().Be(100);
    }

    [Fact]
    public void Should_PreservePageSize_When_PageSizeIsAtMinimum()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 1,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.PageSize.Should().Be(1);
    }

    // ---- SortField validation -----------------------------------------------

    [Fact]
    public void Should_DefaultSortFieldToLastName_When_SortFieldIsUnknown()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: null,
            sortField: "NonExistentField",
            sortDirection: SortDirection.Ascending
        );

        query.SortField.Should().Be("LastName");
    }

    [Fact]
    public void Should_DefaultSortFieldToLastName_When_SortFieldIsNull()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: null,
            sortField: null!,
            sortDirection: SortDirection.Ascending
        );

        query.SortField.Should().Be("LastName");
    }

    [Theory]
    [InlineData("LastName")]
    [InlineData("FirstName")]
    [InlineData("Email")]
    [InlineData("Phone")]
    [InlineData("City")]
    [InlineData("State")]
    [InlineData("DateAdded")]
    public void Should_PreserveSortField_When_SortFieldIsValid(string sortField)
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: null,
            sortField: sortField,
            sortDirection: SortDirection.Ascending
        );

        query.SortField.Should().Be(sortField);
    }

    // ---- SearchText normalization -------------------------------------------

    [Fact]
    public void Should_NormalizeSearchTextToNull_When_SearchTextIsNull()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.SearchText.Should().BeNull();
    }

    [Fact]
    public void Should_NormalizeSearchTextToNull_When_SearchTextIsEmptyString()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: string.Empty,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.SearchText.Should().BeNull();
    }

    [Fact]
    public void Should_NormalizeSearchTextToNull_When_SearchTextIsWhitespaceOnly()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: "   ",
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.SearchText.Should().BeNull();
    }

    [Fact]
    public void Should_TrimSearchText_When_SearchTextHasLeadingAndTrailingWhitespace()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: "  Thor  ",
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.SearchText.Should().Be("Thor");
    }

    [Fact]
    public void Should_PreserveSearchText_When_SearchTextIsAlreadyTrimmed()
    {
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: "Thor",
            sortField: "LastName",
            sortDirection: SortDirection.Ascending
        );

        query.SearchText.Should().Be("Thor");
    }

    // ---- Default constructor ------------------------------------------------

    [Fact]
    public void Should_UseDefaultValues_When_DefaultConstructorIsUsed()
    {
        var query = new PagedQuery();

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(25);
        query.SearchText.Should().BeNull();
        query.SortField.Should().Be("LastName");
        query.SortDirection.Should().Be(SortDirection.Ascending);
    }

    // ---- Sanitized() --------------------------------------------------------

    [Fact]
    public void Sanitized_ReturnsEquivalentQuery_WithSamePropertyValues()
    {
        var original = new PagedQuery(
            page: 2,
            pageSize: 10,
            searchText: "Thor",
            sortField: "Email",
            sortDirection: SortDirection.Descending
        );

        var sanitized = original.Sanitized();

        sanitized.Page.Should().Be(original.Page);
        sanitized.PageSize.Should().Be(original.PageSize);
        sanitized.SearchText.Should().Be(original.SearchText);
        sanitized.SortField.Should().Be(original.SortField);
        sanitized.SortDirection.Should().Be(original.SortDirection);
    }

    [Fact]
    public void Sanitized_ReturnsNewInstance()
    {
        var original = new PagedQuery();

        var sanitized = original.Sanitized();

        sanitized.Should().NotBeSameAs(original);
    }
}
