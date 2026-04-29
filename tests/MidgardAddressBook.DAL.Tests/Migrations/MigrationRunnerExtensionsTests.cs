using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MidgardAddressBook.Core.Caching;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.DAL.Migrations;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Migrations;

/// <summary>
/// Tests for <see cref="MigrationRunnerExtensions"/> covering DI registration shape and
/// the non-DB code paths of <c>RunMidgardMigrationsAsync</c> (argument validation and the
/// success/non-transient-failure branches via a mocked <see cref="IMigrationRunner"/>).
/// </summary>
public class MigrationRunnerExtensionsTests
{
    [Fact]
    public void AddMidgardMigrations_RegistersFluentMigratorRunner()
    {
        var services = new ServiceCollection();

        var returned = services.AddMidgardMigrations(
            "Host=localhost;Username=u;Password=p;Database=d"
        );

        returned.Should().BeSameAs(services);

        // Verify the canonical FluentMigrator services land in the collection.
        services.Should().Contain(s => s.ServiceType == typeof(IMigrationRunner));
        services.Should().Contain(s => s.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void AddMidgardMigrations_Throws_OnNullServices()
    {
        IServiceCollection services = null!;
        Action act = () => services.AddMidgardMigrations("x");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddMidgardMigrations_Throws_OnEmptyConnectionString(string? cs)
    {
        var services = new ServiceCollection();
        Action act = () => services.AddMidgardMigrations(cs!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RunMidgardMigrationsAsync_Throws_OnNullProvider()
    {
        IServiceProvider provider = null!;
        Func<Task> act = () => provider.RunMidgardMigrationsAsync();

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RunMidgardMigrationsAsync_Throws_WhenMaxAttemptsLessThanOne(int attempts)
    {
        await using var provider = BuildProviderWithMockRunner(out _);
        Func<Task> act = async () =>
            await provider.RunMidgardMigrationsAsync(maxAttempts: attempts);

        await act.Should()
            .ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("maxAttempts");
    }

    [Fact]
    public async Task RunMidgardMigrationsAsync_Throws_OnNegativeRetryDelay()
    {
        await using var provider = BuildProviderWithMockRunner(out _);
        Func<Task> act = async () =>
            await provider.RunMidgardMigrationsAsync(retryDelay: TimeSpan.FromSeconds(-1));

        await act.Should()
            .ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("retryDelay");
    }

    [Fact]
    public async Task RunMidgardMigrationsAsync_Calls_MigrateUp_OnSuccess()
    {
        await using var provider = BuildProviderWithMockRunner(out var runner);

        await provider.RunMidgardMigrationsAsync();

        runner.Verify(r => r.MigrateUp(), Times.Once);
    }

    [Fact]
    public async Task RunMidgardMigrationsAsync_Propagates_NonTransientException()
    {
        await using var provider = BuildProviderWithMockRunner(out var runner);
        runner.Setup(r => r.MigrateUp()).Throws(new InvalidOperationException("bad-script"));
        var capturedRunner = runner;

        Func<Task> act = async () => await provider.RunMidgardMigrationsAsync(maxAttempts: 3);

        // Non-transient exceptions are not caught by the retry guard and propagate.
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bad-script");
        capturedRunner.Verify(r => r.MigrateUp(), Times.Once);
    }

    /// <summary>
    /// Builds a minimal service provider with a mock <see cref="IMigrationRunner"/> and a
    /// real <see cref="ILoggerFactory"/> (NullLoggerFactory) so the retry loop can resolve
    /// its dependencies without standing up FluentMigrator's full pipeline.
    /// </summary>
    private static ServiceProvider BuildProviderWithMockRunner(out Mock<IMigrationRunner> runner)
    {
        var localRunner = new Mock<IMigrationRunner>();
        localRunner.Setup(r => r.MigrateUp());
        runner = localRunner;

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddScoped(_ => localRunner.Object);
        return services.BuildServiceProvider();
    }
}

/// <summary>
/// Integration tests for <see cref="MigrationRunnerExtensions.SeedIfRequestedAsync"/> that
/// verify cache-invalidation behaviour using a real PostgreSQL Testcontainer.
/// Each test class instance gets its own fresh container so the seeded/non-seeded scenarios
/// are fully isolated from each other.
/// </summary>
public sealed class SeedIfRequestedAsyncTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18.3-alpine")
        .Build();

    private string _connectionString = string.Empty;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);
        _connectionString = _postgres.GetConnectionString();

        // Apply schema migrations so address_book_entries exists.
        var services = new ServiceCollection();
        services.AddMidgardMigrations(_connectionString);
        await using var provider = services.BuildServiceProvider();
        await provider
            .RunMidgardMigrationsAsync(
                maxAttempts: 3,
                retryDelay: TimeSpan.FromSeconds(2)
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _postgres.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task SeedIfRequestedAsync_WhenTableIsEmpty_SeedsData_AndInvalidatesCache()
    {
        var cacheMock = new Mock<ICacheService>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<ICacheService>(cacheMock.Object);
        await using var provider = services.BuildServiceProvider();

        await provider.SeedIfRequestedAsync(
            seedRequested: true,
            postgresConnectionString: _connectionString
        );

        cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.AddressBookList, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SeedIfRequestedAsync_WhenTableHasRows_SkipsSeeder_AndDoesNotInvalidateCache()
    {
        // Pre-insert one row so the idempotency check skips seeding.
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "INSERT INTO address_book_entries "
                + "(first_name, last_name, email, address1, state, city, zip_code, phone, date_added) "
                + "VALUES ('T','T','t@t.com','1 St','NY','NYC','10001','555-0000000', NOW())"
        );

        var cacheMock = new Mock<ICacheService>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<ICacheService>(cacheMock.Object);
        await using var provider = services.BuildServiceProvider();

        await provider.SeedIfRequestedAsync(
            seedRequested: true,
            postgresConnectionString: _connectionString
        );

        cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
