using FluentAssertions;
using MidgardAddressBook.Core.Models;
using Xunit;

namespace MidgardAddressBook.Core.Tests.Models;

/// <summary>
/// Smoke tests for <see cref="AddressBookEntry"/> ensuring construction and default values work
/// as expected.
/// </summary>
public class AddressBookEntryTests
{
    [Fact]
    public void DefaultsAreEmptyStringsAndZeroId()
    {
        var entry = new AddressBookEntry();

        entry.Id.Should().Be(0);
        entry.FirstName.Should().BeEmpty();
        entry.LastName.Should().BeEmpty();
        entry.Email.Should().BeEmpty();
        entry.Address1.Should().BeEmpty();
        entry.City.Should().BeEmpty();
        entry.State.Should().BeEmpty();
        entry.ZipCode.Should().BeEmpty();
        entry.Phone.Should().BeEmpty();
        entry.Address2.Should().BeNull();
        entry.Avatar.Should().BeNull();
        entry.FileName.Should().BeNull();
    }

    [Fact]
    public void PropertiesRoundTrip()
    {
        var entry = new AddressBookEntry
        {
            Id = 42,
            FirstName = "Thor",
            LastName = "Odinson",
            Email = "thor@asgard.realm",
            Address1 = "1 Bifrost Way",
            City = "Asgard",
            State = "AS",
            ZipCode = "00001",
            Phone = "555-0100",
        };

        entry.Id.Should().Be(42);
        entry.FirstName.Should().Be("Thor");
        entry.LastName.Should().Be("Odinson");
        entry.Email.Should().Be("thor@asgard.realm");
    }

    [Fact]
    public void OptionalProperties_RoundTrip()
    {
        var avatar = new byte[] { 1, 2, 3, 4 };
        var added = new System.DateTimeOffset(2024, 4, 24, 0, 0, 0, System.TimeSpan.Zero);
        var entry = new AddressBookEntry
        {
            Address2 = "Suite 9",
            Avatar = avatar,
            FileName = "thor.png",
            DateAdded = added,
        };

        entry.Address2.Should().Be("Suite 9");
        entry.Avatar.Should().BeSameAs(avatar);
        entry.FileName.Should().Be("thor.png");
        entry.DateAdded.Should().Be(added);
    }
}
