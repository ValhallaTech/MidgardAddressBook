using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Web.Components.Pages;
using Moq;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Pages;

/// <summary>
/// bUnit tests for the <see cref="Home"/> Blazor page covering loading, empty,
/// and populated states plus the delete interaction.
/// </summary>
public class HomeTests : TestContext
{
    private readonly Mock<IAddressBookService> _service = new();

    public HomeTests()
    {
        Services.AddSingleton(_service.Object);
    }

    [Fact]
    public void RendersLoadingPlaceholder_BeforeServiceResponds()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<AddressBookEntryDto>>();
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);

        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("Loading...");
    }

    [Fact]
    public void RendersEmptyState_WhenServiceReturnsNoEntries()
    {
        _service
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AddressBookEntryDto>());

        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("No contacts yet.");
    }

    [Fact]
    public void RendersTableRow_PerEntry()
    {
        _service
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<AddressBookEntryDto>
                {
                    new()
                    {
                        Id = 1,
                        FirstName = "Thor",
                        LastName = "Odinson",
                        Email = "thor@asgard.realm",
                        Phone = "555-0100",
                        City = "Asgard",
                        State = "AS",
                    },
                }
            );

        var cut = RenderComponent<Home>();

        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(1);
        cut.Markup.Should().Contain("Odinson, Thor");
        cut.Markup.Should().Contain("thor@asgard.realm");
        cut.Markup.Should().Contain("Asgard, AS");
        cut.Find("a.btn.btn-primary").GetAttribute("href").Should().Be("/entries/new");
    }

    [Fact]
    public void Delete_InvokesService_AndReloadsList()
    {
        _service
            .SetupSequence(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<AddressBookEntryDto>
                {
                    new()
                    {
                        Id = 5,
                        FirstName = "Loki",
                        LastName = "L",
                        Email = "l@x.com",
                    },
                }
            )
            .ReturnsAsync(new List<AddressBookEntryDto>());
        _service.Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cut = RenderComponent<Home>();
        cut.Find("button.btn-outline-danger").Click();

        _service.Verify(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        cut.Markup.Should().Contain("No contacts yet.");
    }
}
