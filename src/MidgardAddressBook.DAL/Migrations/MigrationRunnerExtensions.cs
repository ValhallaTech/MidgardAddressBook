using System;
using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    /// Errors are logged and rethrown so startup fails fast on schema problems.
    /// </summary>
    /// <param name="serviceProvider">Root service provider.</param>
    public static void RunMidgardMigrations(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}
