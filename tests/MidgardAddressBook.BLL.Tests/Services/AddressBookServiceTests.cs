using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MidgardAddressBook.BLL.Mapping;
using MidgardAddressBook.BLL.Services;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Core.Models;
using Moq;
using Xunit;

namespace MidgardAddressBook.BLL.Tests.Services;

/// <summary>
/// Smoke tests for <see cref="AddressBookService"/>.
/// </summary>
public class AddressBookServiceTests
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
    public async Task CreateAsync_PersistsEntry_StampsDateAdded_AndInvalidatesListCache()
    {
        var repo = new Mock<IAddressBookEntryRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<AddressBookEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        var cache = new Mock<ICacheService>();
        var service = new AddressBookService(
            repo.Object,
            cache.Object,
            CreateMapper(),
            NullLogger<AddressBookService>.Instance
        );

        var dto = new AddressBookEntryDto
        {
            FirstName = "Freya",
            LastName = "Vanir",
            Email = "freya@vanaheim.realm",
            Address1 = "1 Folkvangr",
            City = "Vanaheim",
            State = "VN",
            ZipCode = "00002",
            Phone = "555-0102",
        };

        var result = await service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Freya");
        repo.Verify(
            r =>
                r.CreateAsync(
                    It.Is<AddressBookEntry>(e => e.FirstName == "Freya" && e.DateAdded != default),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        cache.Verify(
            c => c.RemoveAsync("address-book:entries:all", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var repo = new Mock<IAddressBookEntryRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddressBookEntry?)null);
        var cache = new Mock<ICacheService>();
        var service = new AddressBookService(
            repo.Object,
            cache.Object,
            CreateMapper(),
            NullLogger<AddressBookService>.Instance
        );

        var result = await service.GetByIdAsync(99);

        result.Should().BeNull();
    }
}
