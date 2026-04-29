using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
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
}
