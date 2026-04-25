using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MidgardAddressBook.DAL.Migrations;
using Moq;
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
