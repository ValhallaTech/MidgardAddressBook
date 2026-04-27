using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MidgardAddressBook.Core.Models;
using MidgardAddressBook.DAL.Caching;
using MidgardAddressBook.DAL.Configuration;
using MidgardAddressBook.DAL.Repositories;
using Npgsql;
using StackExchange.Redis;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Integration;

/// <summary>
/// Testcontainers-based integration tests for <see cref="AddressBookEntryRepository"/> and
/// <see cref="RedisCacheService"/> against real PostgreSQL and Redis containers.
/// The shared <see cref="IntegrationCollectionFixture"/> starts both containers, applies
/// FluentMigrator migrations, and seeds 1 000 rows before any test runs.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PerformanceDataIntegrationTests
{
    private const int SeedCount = 1000;
    private const string CacheKey = "integration:all-entries";

    private readonly AddressBookEntryRepository _repository;
    private readonly RedisCacheService _cacheService;
    private readonly string _postgresConnectionString;

    /// <summary>
    /// Initializes a new instance of <see cref="PerformanceDataIntegrationTests"/> with the
    /// shared fixture that provides live container connection strings.
    /// </summary>
    /// <param name="fixture">The shared collection fixture providing container endpoints.</param>
    public PerformanceDataIntegrationTests(IntegrationCollectionFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgresConnectionString = fixture.PostgresConnectionString;

        var dataOptions = Options.Create(
            new DataOptions
            {
                PostgresConnectionString = fixture.PostgresConnectionString,
                RedisConnectionString = fixture.RedisConnectionString,
            }
        );

        _repository = new AddressBookEntryRepository(dataOptions);

        var multiplexer = ConnectionMultiplexer.Connect(fixture.RedisConnectionString);
        _cacheService = new RedisCacheService(multiplexer, NullLogger<RedisCacheService>.Instance);
    }

    // ── Repository tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AddressBookEntryRepository.GetAllAsync"/> returns exactly
    /// 1 000 rows and that every row has a non-empty <c>Email</c> value.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns1000SeededRows()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(SeedCount);
        results.Should().AllSatisfy(e => e.Email.Should().NotBeNullOrWhiteSpace());
    }

    /// <summary>
    /// Verifies that <see cref="AddressBookEntryRepository.GetAllAsync"/> completes in
    /// under 5 seconds for a 1 000-row result set.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_CompletesWithin5Seconds()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        _ = await _repository.GetAllAsync();

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that <see cref="AddressBookEntryRepository.GetByIdAsync"/> returns the entry
    /// whose <c>email</c> column equals <c>user0@example.com</c> when queried by its real
    /// database-assigned <c>id</c>.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsExpectedEntry()
    {
        // Arrange — resolve the real id for user0@example.com
        await using var connection = new NpgsqlConnection(_postgresConnectionString);
        var id = await connection.QuerySingleAsync<int>(
            "SELECT id FROM address_book_entries WHERE email = 'user0@example.com'"
        );

        // Act
        var entry = await _repository.GetByIdAsync(id);

        // Assert
        entry.Should().NotBeNull();
        entry!.Email.Should().Be("user0@example.com");
    }

    // ── Redis cache tests ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a round-trip through <see cref="RedisCacheService.SetAsync{T}"/> and
    /// <see cref="RedisCacheService.GetAsync{T}"/> preserves all 1 000 entries.
    /// </summary>
    [Fact]
    public async Task SetAsync_AndGetAsync_RoundTripsLargeList()
    {
        // Arrange
        var allEntries = await _repository.GetAllAsync();

        // Act
        await _cacheService.SetAsync(CacheKey, allEntries.ToList());
        var retrieved = await _cacheService.GetAsync<List<AddressBookEntry>>(CacheKey);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Should().HaveCount(SeedCount);
    }

    /// <summary>
    /// Verifies that a full cache set-then-get cycle for 1 000 entries completes in under
    /// 5 seconds.
    /// </summary>
    [Fact]
    public async Task SetAsync_AndGetAsync_CompletesWithin5Seconds()
    {
        // Arrange
        var allEntries = await _repository.GetAllAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.SetAsync(CacheKey + ":perf", allEntries.ToList());
        _ = await _cacheService.GetAsync<List<AddressBookEntry>>(CacheKey + ":perf");

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
