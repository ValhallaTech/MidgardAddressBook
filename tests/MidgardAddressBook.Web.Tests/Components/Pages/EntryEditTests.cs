using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Web.Components.Pages;
using Moq;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Pages;

/// <summary>
/// bUnit tests for <see cref="EntryEdit"/> covering new/edit modes, server load on edit,
/// and the save → navigate-home flow for both Create and Update paths.
/// </summary>
public class EntryEditTests : BunitContext
{
    private readonly Mock<IAddressBookService> _service = new();

    public EntryEditTests()
    {
        Services.AddSingleton(_service.Object);
    }

    [Fact]
    public void NewMode_RendersEmptyForm_WhenIdIsZeroOrNull()
    {
        var cut = Render<EntryEdit>();

        cut.Markup.Should().Contain("Add New Contact");
        cut.Find("button[type='submit']").TextContent.Should().Contain("Create Contact");
    }

    [Fact]
    public void EditMode_LoadsEntry_FromService()
    {
        _service
            .Setup(s => s.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AddressBookEntryDto
                {
                    Id = 7,
                    FirstName = "Tyr",
                    LastName = "Bravewolf",
                    Email = "tyr@asgard.realm",
                    Address1 = "1 Hall",
                    City = "Asgard",
                    State = "AS",
                    ZipCode = "00007",
                    Phone = "555-0007",
                }
            );

        var cut = Render<EntryEdit>(p => p.Add(c => c.Id, 7));

        cut.Markup.Should().Contain("Edit Contact");
        var inputs = cut.FindAll("input.form-control");
        inputs.Should().NotBeEmpty();
        inputs[0].GetAttribute("value").Should().Be("Tyr");
        _service.Verify(s => s.GetByIdAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void EditMode_FallsBackToBlankDto_WhenServiceReturnsNull()
    {
        _service
            .Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddressBookEntryDto?)null);

        var cut = Render<EntryEdit>(p => p.Add(c => c.Id, 99));

        // Renders form anyway (ID is preserved on the blank fallback).
        cut.Markup.Should().Contain("Edit Contact");
    }

    [Fact]
    public void Save_InvokesCreateAsync_AndNavigatesHome_InNewMode()
    {
        _service
            .Setup(s =>
                s.CreateAsync(It.IsAny<AddressBookEntryDto>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (AddressBookEntryDto d, CancellationToken _) =>
                {
                    d.Id = 100;
                    return d;
                }
            );
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<EntryEdit>();
        FillRequiredFields(cut);
        cut.Find("form").Submit();

        _service.Verify(
            s =>
                s.CreateAsync(
                    It.Is<AddressBookEntryDto>(d => d.FirstName == "Vidar"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        nav.Uri.Should().EndWith("/");
    }

    [Fact]
    public void Save_InvokesUpdateAsync_AndNavigatesHome_InEditMode()
    {
        _service
            .Setup(s => s.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AddressBookEntryDto
                {
                    Id = 8,
                    FirstName = "Old",
                    LastName = "Name",
                    Email = "old@x.com",
                    Address1 = "old",
                    City = "old",
                    State = "old",
                    ZipCode = "00000",
                    Phone = "000",
                }
            );
        _service
            .Setup(s =>
                s.UpdateAsync(It.IsAny<AddressBookEntryDto>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<EntryEdit>(p => p.Add(c => c.Id, 8));
        cut.Find("form").Submit();

        _service.Verify(
            s =>
                s.UpdateAsync(
                    It.Is<AddressBookEntryDto>(d => d.Id == 8),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        nav.Uri.Should().EndWith("/");
    }

    private static void FillRequiredFields(IRenderedComponent<EntryEdit> cut)
    {
        // Re-find on every interaction: each Change() triggers a re-render that
        // invalidates prior event-handler IDs (see Bunit.UnknownEventHandlerIdException).
        // The markup order is: FirstName, LastName, Email, Phone, Address1,
        // Address2, City, State, ZipCode.
        void Set(int index, string value) => cut.FindAll("input.form-control")[index].Change(value);

        Set(0, "Vidar");
        Set(1, "Silentgod");
        Set(2, "vidar@asgard.realm");
        Set(3, "555-0011");
        Set(4, "1 Forest");
        Set(6, "Vidi");
        Set(7, "AS");
        Set(8, "00011");
    }
}
