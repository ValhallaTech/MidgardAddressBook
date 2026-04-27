using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection;
using MidgardAddressBook.DAL.Migrations;
using MidgardAddressBook.DAL.Seeding;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Integration;

/// <summary>
/// Marks the "Integration" collection so that all classes decorated with
/// <c>[Collection("Integration")]</c> share the same <see cref="IntegrationCollectionFixture"/>
/// instance.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationCollectionFixture>
{
    // This class is intentionally empty.
    // Its purpose is to be the target of [CollectionDefinition] and [ICollectionFixture<T>].
}

/// <summary>
/// Shared fixture for all integration tests in the "Integration" collection.
/// Starts a PostgreSQL container and a Redis container, applies FluentMigrator schema
/// migrations, and seeds 1 000 deterministic rows using <see cref="DatabaseSeeder"/>.
/// Both containers are started once per test collection run and disposed together.
/// </summary>
public sealed class IntegrationCollectionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18.3-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:8-alpine")
        .Build();

    /// <summary>
    /// Gets the fully-formed Npgsql connection string for the running PostgreSQL container.
    /// Available after <see cref="InitializeAsync"/> has completed.
    /// </summary>
    public string PostgresConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the StackExchange.Redis configuration string (host:port) for the running Redis
    /// container. Available after <see cref="InitializeAsync"/> has completed.
    /// </summary>
    public string RedisConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Starts both containers, runs schema migrations, and seeds 1 000 rows.
    /// Called once before any test in the collection executes.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync()
        ).ConfigureAwait(false);

        PostgresConnectionString = _postgres.GetConnectionString();
        RedisConnectionString = _redis.GetConnectionString();

        // Apply FluentMigrator schema migrations against the fresh database.
        var services = new ServiceCollection();
        services.AddMidgardMigrations(PostgresConnectionString);
        await using var provider = services.BuildServiceProvider();
        await provider.RunMidgardMigrationsAsync(
            maxAttempts: 3,
            retryDelay: System.TimeSpan.FromSeconds(2)
        ).ConfigureAwait(false);

        // Seed 1 000 deterministic rows.
        var seeder = new DatabaseSeeder(PostgresConnectionString);
        await seeder.SeedAsync(1000).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops and disposes both containers.
    /// Called once after all tests in the collection have executed.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await _redis.DisposeAsync().ConfigureAwait(false);
    }
}
