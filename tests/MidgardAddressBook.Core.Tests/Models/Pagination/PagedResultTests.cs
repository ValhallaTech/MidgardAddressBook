using FluentAssertions;
using MidgardAddressBook.Core.Models.Pagination;
using Xunit;

namespace MidgardAddressBook.Core.Tests.Models.Pagination;

/// <summary>
/// Tests for <see cref="PagedResult{T}"/> covering the <see cref="PagedResult{T}.TotalPages"/>
/// computed property under various boundary conditions.
/// </summary>
public class PagedResultTests
{
    // ---- TotalPages computation ---------------------------------------------

    [Fact]
    public void Should_CalculateTotalPages_When_ItemsDivideEvenly()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 50,
            PageSize = 10,
            Page = 1,
        };

        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public void Should_RoundUpTotalPages_When_ItemsDontDivideEvenly()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 51,
            PageSize = 10,
            Page = 1,
        };

        result.TotalPages.Should().Be(6);
    }

    [Fact]
    public void Should_ReturnOneTotalPage_When_TotalCountEqualsPageSize()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 25,
            PageSize = 25,
            Page = 1,
        };

        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Should_ReturnOneTotalPage_When_TotalCountIsLessThanPageSize()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 3,
            PageSize = 25,
            Page = 1,
        };

        result.TotalPages.Should().Be(1);
    }

    // ---- Zero/empty boundary conditions ------------------------------------

    [Fact]
    public void Should_ReturnZeroTotalPages_When_TotalCountIsZero()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 0,
            PageSize = 25,
            Page = 1,
        };

        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void Should_ReturnZeroTotalPages_When_PageSizeIsZero()
    {
        // Guards against divide-by-zero: the property returns 0 when PageSize == 0.
        var result = new PagedResult<string>
        {
            TotalCount = 100,
            PageSize = 0,
            Page = 1,
        };

        result.TotalPages.Should().Be(0);
    }

    // ---- Default state ------------------------------------------------------

    [Fact]
    public void Items_DefaultsToEmptyList()
    {
        var result = new PagedResult<string>();

        result.Items.Should().BeEmpty();
    }
}
