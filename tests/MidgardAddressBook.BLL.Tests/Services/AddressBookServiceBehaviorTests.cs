using System;
using System.Collections.Generic;
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
/// Comprehensive coverage for <see cref="AddressBookService"/> — cache hit/miss/invalidation
/// pathways, repository delegation, mapping, null-arg guards, and cancellation propagation.
/// </summary>
public class AddressBookServiceBehaviorTests
{
    private const string ListCacheKey = "address-book:entries:all";

    private readonly Mock<IAddressBookEntryRepository> _repo = new(MockBehavior.Strict);
    private readonly FakeCacheService _cache = new();
    private readonly IMapper _mapper;

    public AddressBookServiceBehaviorTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AddressBookEntryProfile>(),
            NullLoggerFactory.Instance
        );
        _mapper = config.CreateMapper();
    }

    private AddressBookService CreateSut() =>
        new(_repo.Object, _cache, _mapper, NullLogger<AddressBookService>.Instance);

    // ---- Constructor null guards -----------------------------------------

    [Fact]
    public void Constructor_Throws_WhenRepositoryIsNull()
    {
        Action act = () =>
            _ = new AddressBookService(
                null!,
                _cache,
                _mapper,
                NullLogger<AddressBookService>.Instance
            );

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_Throws_WhenCacheIsNull()
    {
        Action act = () =>
            _ = new AddressBookService(
                _repo.Object,
                null!,
                _mapper,
                NullLogger<AddressBookService>.Instance
            );

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_Throws_WhenMapperIsNull()
    {
        Action act = () =>
            _ = new AddressBookService(
                _repo.Object,
                _cache,
                null!,
                NullLogger<AddressBookService>.Instance
            );

        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
    }

    [Fact]
    public void Constructor_Throws_WhenLoggerIsNull()
    {
        Action act = () => _ = new AddressBookService(_repo.Object, _cache, _mapper, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ---- GetAllAsync -------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_OnCacheMiss_LoadsFromRepository_AndPopulatesCache()
    {
        var entities = new List<AddressBookEntry>
        {
            new()
            {
                Id = 1,
                FirstName = "A",
                LastName = "Z",
            },
            new()
            {
                Id = 2,
                FirstName = "B",
                LastName = "Y",
            },
        };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entities);

        var sut = CreateSut();
        var result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("A");
        _cache.Sets.Should().ContainSingle().Which.Key.Should().Be(ListCacheKey);
        _cache.Sets[0].Ttl.Should().Be(TimeSpan.FromMinutes(5));
        _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_OnCacheHit_ServesFromCache_WithoutHittingRepository()
    {
        var entities = new List<AddressBookEntry>
        {
            new() { Id = 1, FirstName = "Skadi" },
        };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entities);

        var sut = CreateSut();

        // First call: cache miss → repo hit → populates cache
        var first = await sut.GetAllAsync();
        first.Should().HaveCount(1);

        // Second call: cache hit → no additional repo call
        var second = await sut.GetAllAsync();
        second.Should().HaveCount(1);
        second[0].FirstName.Should().Be("Skadi");

        _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_PropagatesCancellationToken_ToRepositoryAndCache()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        _repo.Setup(r => r.GetAllAsync(token)).ReturnsAsync(new List<AddressBookEntry>());

        var sut = CreateSut();
        await sut.GetAllAsync(token);

        _repo.Verify(r => r.GetAllAsync(token), Times.Once);
        _cache.ObservedTokens.Should().Contain(token);
    }

    // ---- GetByIdAsync ------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_OnHit_ReturnsMappedDto()
    {
        var entity = new AddressBookEntry
        {
            Id = 4,
            FirstName = "Heimdall",
            LastName = "Watcher",
            Email = "heim@asgard.realm",
        };
        _repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(4);

        result.Should().NotBeNull();
        result!.Id.Should().Be(4);
        result.FirstName.Should().Be("Heimdall");
    }

    [Fact]
    public async Task GetByIdAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        _repo.Setup(r => r.GetByIdAsync(1, token)).ReturnsAsync((AddressBookEntry?)null);

        var sut = CreateSut();
        await sut.GetByIdAsync(1, token);

        _repo.Verify(r => r.GetByIdAsync(1, token), Times.Once);
    }

    // ---- CreateAsync -------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Throws_OnNullDto()
    {
        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_StampsDateAdded_AndInvalidatesCache()
    {
        AddressBookEntry? captured = null;
        _repo
            .Setup(r => r.CreateAsync(It.IsAny<AddressBookEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AddressBookEntry, CancellationToken>((e, _) => captured = e)
            .ReturnsAsync(11);

        // Pre-populate the cache to verify invalidation actually happens.
        _cache.Storage[ListCacheKey] = new object();

        var dto = new AddressBookEntryDto { FirstName = "Bragi" };
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var sut = CreateSut();
        var result = await sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.DateAdded.Should().BeOnOrAfter(before);
        result.FirstName.Should().Be("Bragi");
        _cache.RemovedKeys.Should().Contain(ListCacheKey);
        _cache.Storage.Should().NotContainKey(ListCacheKey);
    }

    // ---- UpdateAsync -------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_Throws_OnNullDto()
    {
        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenEntityMissing_AndDoesNotInvalidateCache()
    {
        _repo
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddressBookEntry?)null);

        var sut = CreateSut();
        var result = await sut.UpdateAsync(new AddressBookEntryDto { Id = 99 });

        result.Should().BeFalse();
        _cache.RemovedKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_OnSuccess_InvalidatesListCache_AndAppliesScalarMapping()
    {
        var existing = new AddressBookEntry
        {
            Id = 3,
            FirstName = "old",
            LastName = "old",
            Email = "old@x.com",
            Address1 = "old",
            City = "old",
            State = "old",
            ZipCode = "00000",
            Phone = "000",
        };
        _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cache.Storage[ListCacheKey] = new object();

        var sut = CreateSut();
        var result = await sut.UpdateAsync(
            new AddressBookEntryDto
            {
                Id = 3,
                FirstName = "new",
                LastName = "new",
                Email = "new@x.com",
                Address1 = "new",
                City = "new",
                State = "new",
                ZipCode = "00001",
                Phone = "111",
            }
        );

        result.Should().BeTrue();
        existing.FirstName.Should().Be("new", "DTO→entity mapping should overwrite scalar fields");
        _cache.RemovedKeys.Should().Contain(ListCacheKey);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotInvalidateCache_WhenRepositoryReturnsFalse()
    {
        var existing = new AddressBookEntry { Id = 3 };
        _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo
            .Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.UpdateAsync(new AddressBookEntryDto { Id = 3 });

        result.Should().BeFalse();
        _cache.RemovedKeys.Should().BeEmpty();
    }

    // ---- DeleteAsync -------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_OnSuccess_InvalidatesListCache()
    {
        _repo.Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cache.Storage[ListCacheKey] = new object();

        var sut = CreateSut();
        var result = await sut.DeleteAsync(7);

        result.Should().BeTrue();
        _cache.RemovedKeys.Should().Contain(ListCacheKey);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotInvalidateCache_WhenRepositoryReturnsFalse()
    {
        _repo.Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.DeleteAsync(7);

        result.Should().BeFalse();
        _cache.RemovedKeys.Should().BeEmpty();
    }
}
