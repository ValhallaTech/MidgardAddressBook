using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MidgardAddressBook.DAL.Migrations;

/// <summary>
/// Extension helpers for wiring FluentMigrator into an application's service collection and
/// executing migrations at startup.
/// </summary>
public static class MigrationRunnerExtensions
{
    /// <summary>
    /// Registers FluentMigrator with the Postgres runner and scans this assembly for migrations.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="postgresConnectionString">Npgsql connection string to migrate against.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMidgardMigrations(this IServiceCollection services, string postgresConnectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(postgresConnectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    /// <summary>
    /// Runs all pending migrations using a scope-bound <see cref="IMigrationRunner"/>.
    /// Retries up to <paramref name="maxAttempts"/> times with a fixed delay between attempts
    /// to handle transient connectivity failures that can occur when the database service is
    /// still starting up (e.g. first deploy on Render). Only transient
    /// <see cref="NpgsqlException"/> failures trigger a retry; non-transient exceptions
    /// (e.g. a broken migration script) propagate immediately.
    /// </summary>
    /// <param name="serviceProvider">Root service provider.</param>
    /// <param name="maxAttempts">Maximum number of attempts before re-throwing. Must be at least 1. Defaults to 5.</param>
    /// <param name="retryDelay">Delay between attempts. Must be non-negative. Defaults to 10 seconds.</param>
    /// <param name="cancellationToken">Token used to cancel the retry loop.</param>
    public static async Task RunMidgardMigrationsAsync(
        this IServiceProvider serviceProvider,
        int maxAttempts = 5,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        var delay = retryDelay ?? TimeSpan.FromSeconds(10);
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be non-negative.");
        }

        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MidgardAddressBook.DAL.Migrations");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientException(ex))
            {
                logger.LogWarning(
                    ex,
                    "Database migration failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}…",
                    attempt, maxAttempts, delay);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="ex"/> (or any exception in its inner-exception
    /// chain) is a transient <see cref="NpgsqlException"/> — i.e. one that is worth retrying,
    /// such as a connection-refused or network-timeout failure during startup.
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }
}
