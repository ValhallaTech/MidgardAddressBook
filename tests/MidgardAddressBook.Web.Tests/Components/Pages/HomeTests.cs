using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Core.Models.Pagination;
using MidgardAddressBook.Web.Components.Pages;
using Moq;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Pages;

/// <summary>
/// bUnit tests for the <see cref="Home"/> Blazor page covering loading, empty,
/// and populated states; the delete interaction; contact-count display; and search.
/// </summary>
public class HomeTests : BunitContext
{
    private readonly Mock<IAddressBookService> _service = new();

    public HomeTests()
    {
        Services.AddSingleton(_service.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PagedResult<AddressBookEntryDto> EmptyPagedResult() =>
        new() { Items = [], TotalCount = 0, Page = 1, PageSize = 25 };

    private static PagedResult<AddressBookEntryDto> PagedResultWithItems(
        params AddressBookEntryDto[] items
    ) =>
        new()
        {
            Items = items,
            TotalCount = items.Length,
            Page = 1,
            PageSize = 25,
        };

    // ── Rendering states ──────────────────────────────────────────────────────

    [Fact]
    public void RendersLoadingPlaceholder_BeforeServiceResponds()
    {
        var tcs = new TaskCompletionSource<PagedResult<AddressBookEntryDto>>();
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var cut = Render<Home>();

        cut.Markup.Should().Contain("Loading contacts");
    }

    [Fact]
    public void RendersEmptyState_WhenServiceReturnsNoEntries()
    {
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPagedResult());

        var cut = Render<Home>();

        cut.Markup.Should().Contain("No contacts yet");
    }

    [Fact]
    public void RendersTableRow_PerEntry()
    {
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                PagedResultWithItems(
                    new AddressBookEntryDto
                    {
                        Id = 1,
                        FirstName = "Thor",
                        LastName = "Odinson",
                        Email = "thor@asgard.realm",
                        Phone = "555-0100",
                        City = "Asgard",
                        State = "AS",
                    }
                )
            );

        var cut = Render<Home>();

        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(1);
        cut.Markup.Should().Contain("Odinson, Thor");
        cut.Markup.Should().Contain("thor@asgard.realm");
        cut.Markup.Should().Contain("Asgard, AS");
        cut.Find("a.btn.btn-primary").GetAttribute("href").Should().Be("/entries/new");
    }

    // ── Delete interaction ────────────────────────────────────────────────────

    [Fact]
    public void Delete_InvokesService_AndReloadsList()
    {
        _service
            .SetupSequence(s =>
                s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                PagedResultWithItems(
                    new AddressBookEntryDto
                    {
                        Id = 5,
                        FirstName = "Loki",
                        LastName = "L",
                        Email = "l@x.com",
                    }
                )
            )
            .ReturnsAsync(EmptyPagedResult());
        _service
            .Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<Home>();
        cut.Find("button.btn-outline-danger").Click();

        _service.Verify(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(
            s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
        cut.Markup.Should().Contain("No contacts yet");
    }

    // ── Pagination metadata ───────────────────────────────────────────────────

    [Fact]
    public void RendersContactCount_FromTotalCount()
    {
        // Return 1 item on this page but TotalCount = 42 (simulating a multi-page result set).
        // This proves the card header reads TotalCount rather than Items.Count.
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PagedResult<AddressBookEntryDto>
                {
                    Items =
                    [
                        new AddressBookEntryDto
                        {
                            Id = 1,
                            FirstName = "Thor",
                            LastName = "Odinson",
                            Email = "thor@asgard.realm",
                        },
                    ],
                    TotalCount = 42,
                    Page = 1,
                    PageSize = 25,
                }
            );

        var cut = Render<Home>();

        cut.Find(".card-header span").TextContent.Should().Contain("42");
        cut.Find(".card-header span").TextContent.Should().Contain("contacts");
    }

    // ── Search / filter ───────────────────────────────────────────────────────

    [Fact]
    public void Search_ResetsToPage1_AndReloads()
    {
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPagedResult());

        var cut = Render<Home>();

        // Simulate the user typing into the search box.
        cut.Find("input[type='search']").Input("Thor");

        // The component debounces for 300 ms before calling GetPagedAsync.
        // WaitForAssertion polls until the assertion passes or the timeout elapses.
        cut.WaitForAssertion(
            () =>
                _service.Verify(
                    s =>
                        s.GetPagedAsync(
                            It.Is<PagedQuery>(q => q.SearchText == "Thor" && q.Page == 1),
                            It.IsAny<CancellationToken>()
                        ),
                    Times.AtLeastOnce
                ),
            TimeSpan.FromSeconds(2)
        );
    }

    // ── Page-size change ──────────────────────────────────────────────────────

    [Fact]
    public void Should_ReloadWithNewPageSize_When_PageSizeSelectChanges()
    {
        // Arrange
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPagedResult());

        var cut = Render<Home>();

        // Act – change the per-page selector; @bind + @bind:after fires OnPageSizeChangedAsync.
        cut.Find("#pageSizeSelect").Change("10");

        // Assert
        _service.Verify(
            s =>
                s.GetPagedAsync(
                    It.Is<PagedQuery>(q => q.PageSize == 10 && q.Page == 1),
                    It.IsAny<CancellationToken>()
                ),
            Times.AtLeastOnce
        );
    }

    // ── Enter-key search ──────────────────────────────────────────────────────

    [Fact]
    public void Should_ExecuteSearch_When_EnterKeyPressedInSearchInput()
    {
        // Arrange
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPagedResult());

        var cut = Render<Home>();
        var searchInput = cut.Find("input[type='search']");

        // Act – type the query then press Enter; Enter cancels the debounce and
        // calls ExecuteSearchAsync immediately.
        searchInput.Input("whatever");
        searchInput.KeyDown(Key.Enter);

        // Assert – WaitForAssertion covers any residual async scheduling.
        cut.WaitForAssertion(
            () =>
                _service.Verify(
                    s =>
                        s.GetPagedAsync(
                            It.Is<PagedQuery>(q => q.SearchText == "whatever" && q.Page == 1),
                            It.IsAny<CancellationToken>()
                        ),
                    Times.AtLeastOnce
                ),
            TimeSpan.FromSeconds(2)
        );
    }

    // ── Magnifying-glass button search ────────────────────────────────────────

    [Fact]
    public void Should_ExecuteSearch_When_SearchButtonClicked()
    {
        // Arrange
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPagedResult());

        var cut = Render<Home>();

        // Act – type the query then click the magnifying-glass button.
        cut.Find("input[type='search']").Input("whatever");
        cut.Find("button[aria-label='Search']").Click();

        // Assert – WaitForAssertion covers any residual async scheduling from the
        // earlier oninput debounce that was cancelled by the button click.
        cut.WaitForAssertion(
            () =>
                _service.Verify(
                    s =>
                        s.GetPagedAsync(
                            It.Is<PagedQuery>(q => q.SearchText == "whatever" && q.Page == 1),
                            It.IsAny<CancellationToken>()
                        ),
                    Times.AtLeastOnce
                ),
            TimeSpan.FromSeconds(2)
        );
    }

    // ── Sort three-state toggle ───────────────────────────────────────────────

    [Fact]
    public void Should_CycleSort_When_NameColumnHeaderClickedThreeTimes()
    {
        // Arrange – the table must be rendered (non-empty) so column header buttons appear.
        _service
            .Setup(s => s.GetPagedAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                PagedResultWithItems(
                    new AddressBookEntryDto
                    {
                        Id = 1,
                        FirstName = "Thor",
                        LastName = "Odinson",
                        Email = "thor@asgard.realm",
                    }
                )
            );

        var cut = Render<Home>();

        // The component initialises with LastName/Ascending/active.  Click the Email
        // column header first to shift the active sort away from LastName; this puts
        // the state machine in "different field" mode so the three subsequent Name
        // clicks exercise the full Ascending → Descending → no-sort cycle.
        //
        // Column order in the table thead (mirrors the razor markup):
        //   [0] Name/LastName  [1] Email  [2] Phone  [3] City,State  [4] DateAdded
        var thButtons = cut.FindAll("th button.btn-link");
        var emailSortButton = thButtons[1]; // Email column header

        emailSortButton.Click(); // activate Email sort (moves away from default LastName)

        // --- Click 1: different field → LastName/Ascending ---
        cut.FindAll("th button.btn-link")[0].Click();

        _service.Verify(
            s =>
                s.GetPagedAsync(
                    It.Is<PagedQuery>(
                        q =>
                            q.SortField == "LastName"
                            && q.SortDirection == SortDirection.Ascending
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.AtLeastOnce
        );

        // --- Click 2: Ascending → Descending ---
        cut.FindAll("th button.btn-link")[0].Click(); // Name/LastName column, index 0

        _service.Verify(
            s =>
                s.GetPagedAsync(
                    It.Is<PagedQuery>(
                        q =>
                            q.SortField == "LastName"
                            && q.SortDirection == SortDirection.Descending
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.AtLeastOnce
        );

        // --- Click 3: Descending → no sort (component falls back to LastName/Ascending in query) ---
        cut.FindAll("th button.btn-link")[0].Click(); // Name/LastName column, index 0

        // At this point GetPagedAsync(LastName, Ascending) has been called at least twice:
        // once on initial render and once for the click-1 Ascending reload.
        _service.Verify(
            s =>
                s.GetPagedAsync(
                    It.Is<PagedQuery>(
                        q =>
                            q.SortField == "LastName"
                            && q.SortDirection == SortDirection.Ascending
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.AtLeast(2)
        );
    }
}
