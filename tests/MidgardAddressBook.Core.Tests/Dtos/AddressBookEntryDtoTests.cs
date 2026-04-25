using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MidgardAddressBook.Core.Dtos;
using Xunit;

namespace MidgardAddressBook.Core.Tests.Dtos;

/// <summary>
/// Tests for <see cref="AddressBookEntryDto"/> covering defaults, round-trip, and the
/// DataAnnotations validation surface that drives the Blazor form.
/// </summary>
public class AddressBookEntryDtoTests
{
    [Fact]
    public void Defaults_AreEmptyStringsAndZeroId()
    {
        var dto = new AddressBookEntryDto();

        dto.Id.Should().Be(0);
        dto.FirstName.Should().BeEmpty();
        dto.LastName.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.Address1.Should().BeEmpty();
        dto.Address2.Should().BeNull();
        dto.City.Should().BeEmpty();
        dto.State.Should().BeEmpty();
        dto.ZipCode.Should().BeEmpty();
        dto.Phone.Should().BeEmpty();
        dto.DateAdded.Should().Be(default);
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var when = new DateTimeOffset(2024, 4, 24, 12, 0, 0, TimeSpan.Zero);
        var dto = new AddressBookEntryDto
        {
            Id = 13,
            FirstName = "Loki",
            LastName = "Laufeyson",
            Email = "loki@asgard.realm",
            Address1 = "13 Trickster Way",
            Address2 = "Apt 2",
            City = "Asgard",
            State = "AS",
            ZipCode = "00013",
            Phone = "555-0113",
            DateAdded = when,
        };

        dto.Id.Should().Be(13);
        dto.Address2.Should().Be("Apt 2");
        dto.DateAdded.Should().Be(when);
    }

    [Fact]
    public void Validation_Passes_ForFullyPopulatedDto()
    {
        var dto = ValidDto();

        var results = Validate(dto);

        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(nameof(AddressBookEntryDto.FirstName))]
    [InlineData(nameof(AddressBookEntryDto.LastName))]
    [InlineData(nameof(AddressBookEntryDto.Email))]
    [InlineData(nameof(AddressBookEntryDto.Address1))]
    [InlineData(nameof(AddressBookEntryDto.City))]
    [InlineData(nameof(AddressBookEntryDto.State))]
    [InlineData(nameof(AddressBookEntryDto.ZipCode))]
    [InlineData(nameof(AddressBookEntryDto.Phone))]
    public void Validation_Fails_WhenRequiredFieldIsMissing(string property)
    {
        var dto = ValidDto();
        typeof(AddressBookEntryDto).GetProperty(property)!.SetValue(dto, string.Empty);

        var results = Validate(dto);

        results
            .Should()
            .Contain(r => r.MemberNames.Contains(property), $"{property} is [Required]");
    }

    [Fact]
    public void Validation_Fails_WhenEmailIsMalformed()
    {
        var dto = ValidDto();
        dto.Email = "not-an-email";

        var results = Validate(dto);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(AddressBookEntryDto.Email)));
    }

    [Fact]
    public void Validation_Fails_WhenStringLengthExceeded()
    {
        var dto = ValidDto();
        dto.FirstName = new string('x', 101);

        var results = Validate(dto);

        results
            .Should()
            .Contain(r => r.MemberNames.Contains(nameof(AddressBookEntryDto.FirstName)));
    }

    [Fact]
    public void Address2_IsOptional()
    {
        var dto = ValidDto();
        dto.Address2 = null;

        var results = Validate(dto);

        results.Should().BeEmpty();
    }

    private static AddressBookEntryDto ValidDto() =>
        new()
        {
            FirstName = "Sif",
            LastName = "Asgardian",
            Email = "sif@asgard.realm",
            Address1 = "1 Golden Hall",
            City = "Asgard",
            State = "AS",
            ZipCode = "00099",
            Phone = "555-0199",
        };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            results,
            validateAllProperties: true
        );
        return results;
    }
}
