using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MidgardAddressBook.BLL.Mapping;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Models;
using Xunit;

namespace MidgardAddressBook.BLL.Tests.Mapping;

/// <summary>
/// Tests for <see cref="AddressBookEntryProfile"/> covering bidirectional mapping and
/// the AutoMapper configuration validity check.
/// </summary>
public class AddressBookEntryProfileTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AddressBookEntryProfile>(),
            NullLoggerFactory.Instance
        );
        return config.CreateMapper();
    }

    [Fact]
    public void Configuration_IsValid()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AddressBookEntryProfile>(),
            NullLoggerFactory.Instance
        );

        config.Invoking(c => c.AssertConfigurationIsValid()).Should().NotThrow();
    }

    [Fact]
    public void Maps_EntityToDto_CopiesAllScalarFields()
    {
        var mapper = CreateMapper();
        var entry = new AddressBookEntry
        {
            Id = 1,
            FirstName = "Odin",
            LastName = "Allfather",
            Email = "odin@asgard.realm",
            Avatar = new byte[] { 9, 9 },
            FileName = "odin.png",
            Address1 = "1 Hliðskjálf",
            Address2 = "Throne Room",
            State = "AS",
            City = "Asgard",
            ZipCode = "00000",
            Phone = "555-0001",
            DateAdded = new System.DateTimeOffset(2024, 1, 1, 0, 0, 0, System.TimeSpan.Zero),
        };

        var dto = mapper.Map<AddressBookEntryDto>(entry);

        dto.Id.Should().Be(entry.Id);
        dto.FirstName.Should().Be(entry.FirstName);
        dto.LastName.Should().Be(entry.LastName);
        dto.Email.Should().Be(entry.Email);
        dto.Address1.Should().Be(entry.Address1);
        dto.Address2.Should().Be(entry.Address2);
        dto.City.Should().Be(entry.City);
        dto.State.Should().Be(entry.State);
        dto.ZipCode.Should().Be(entry.ZipCode);
        dto.Phone.Should().Be(entry.Phone);
        dto.DateAdded.Should().Be(entry.DateAdded);
    }

    [Fact]
    public void Maps_DtoToEntity_IgnoresAvatarFileNameAndDateAdded()
    {
        var mapper = CreateMapper();
        var dto = new AddressBookEntryDto
        {
            Id = 5,
            FirstName = "Frigg",
            LastName = "Asgardian",
            Email = "frigg@asgard.realm",
            Address1 = "2 Fensalir",
            Address2 = "Hall A",
            City = "Asgard",
            State = "AS",
            ZipCode = "00005",
            Phone = "555-0005",
            DateAdded = new System.DateTimeOffset(2024, 6, 1, 0, 0, 0, System.TimeSpan.Zero),
        };

        var entity = mapper.Map<AddressBookEntry>(dto);

        entity.Id.Should().Be(5);
        entity.FirstName.Should().Be("Frigg");
        entity.Address2.Should().Be("Hall A");
        entity.Avatar.Should().BeNull("Avatar is explicitly ignored on DTO→entity");
        entity.FileName.Should().BeNull("FileName is explicitly ignored on DTO→entity");
        entity
            .DateAdded.Should()
            .Be(default, "DateAdded is ignored to preserve the DB-stored timestamp");
    }

    [Fact]
    public void Maps_DtoToExistingEntity_PreservesAvatarFileNameAndDateAdded()
    {
        var mapper = CreateMapper();
        var existing = new AddressBookEntry
        {
            Id = 9,
            FirstName = "old",
            LastName = "old",
            Email = "old@example.com",
            Address1 = "old",
            City = "old",
            State = "old",
            ZipCode = "old",
            Phone = "old",
            Avatar = new byte[] { 1, 2, 3 },
            FileName = "keep.png",
            DateAdded = new System.DateTimeOffset(2020, 1, 1, 0, 0, 0, System.TimeSpan.Zero),
        };
        var dto = new AddressBookEntryDto
        {
            Id = 9,
            FirstName = "new",
            LastName = "new",
            Email = "new@example.com",
            Address1 = "new",
            City = "new",
            State = "new",
            ZipCode = "new",
            Phone = "new",
        };

        mapper.Map(dto, existing);

        existing.FirstName.Should().Be("new");
        existing.Email.Should().Be("new@example.com");
        existing.Avatar.Should().NotBeNull().And.Equal(new byte[] { 1, 2, 3 });
        existing.FileName.Should().Be("keep.png");
        existing
            .DateAdded.Should()
            .Be(new System.DateTimeOffset(2020, 1, 1, 0, 0, 0, System.TimeSpan.Zero));
    }
}
