using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace MidgardAddressBook.DAL.Seeding;

/// <summary>
/// Bulk-inserts deterministic synthetic <c>address_book_entries</c> rows for performance and
/// load testing. Intended for direct instantiation by test harnesses and tooling — not
/// registered with the application's DI container.
/// </summary>
/// <remarks>
/// All rows are generated deterministically from the row index, so successive seeding runs
/// produce predictable, reproducible data sets. A single <c>unnest</c>-based
/// <c>INSERT … SELECT</c> statement is used to minimise round-trips to the database.
/// </remarks>
public sealed class DatabaseSeeder
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new <see cref="DatabaseSeeder"/> instance.
    /// </summary>
    /// <param name="connectionString">
    /// Npgsql connection string targeting the PostgreSQL database to seed.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connectionString"/> is <see langword="null"/>, empty, or
    /// whitespace.
    /// </exception>
    public DatabaseSeeder(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Bulk-inserts <paramref name="count"/> deterministic synthetic rows into
    /// <c>address_book_entries</c> using a single <c>unnest</c>-based
    /// <c>INSERT … SELECT</c> statement.
    /// </summary>
    /// <param name="count">
    /// Number of rows to insert. Must be greater than zero. Defaults to <c>1 000</c>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> that completes when the insert has finished.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is less than <c>1</c>.
    /// </exception>
    public async Task SeedAsync(int count = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        // Build parallel arrays — one element per row. PostgreSQL's unnest() expands them
        // in lockstep, producing exactly `count` rows from a single statement.
        var firstNames = new string[count];
        var lastNames = new string[count];
        var emails = new string[count];
        var addresses1 = new string[count];
        var states = new string[count];
        var cities = new string[count];
        var zipCodes = new string[count];
        var phones = new string[count];
        var datesAdded = new DateTimeOffset[count];

        for (var i = 0; i < count; i++)
        {
            firstNames[i] = $"First{i}";
            lastNames[i] = $"Last{i}";
            emails[i] = $"user{i}@example.com";
            addresses1[i] = $"{i} Test Street";
            states[i] = "NY";
            cities[i] = "Midgard";
            zipCodes[i] = "10001";
            phones[i] = $"555-{i:D7}";
            datesAdded[i] = DateTimeOffset.UtcNow.AddDays(-i);
        }

        // address2 and avatar / file_name are intentionally omitted — they will be NULL,
        // which matches the nullable column definitions and keeps the seeder simple.
        const string sql = """
            INSERT INTO address_book_entries
                (first_name, last_name, email, address1, state, city, zip_code, phone, date_added)
            SELECT * FROM unnest(
                @FirstNames::varchar[],
                @LastNames::varchar[],
                @Emails::varchar[],
                @Addresses1::varchar[],
                @States::varchar[],
                @Cities::varchar[],
                @ZipCodes::varchar[],
                @Phones::varchar[],
                @DatesAdded::timestamptz[]
            );
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            sql,
            new
            {
                FirstNames = firstNames,
                LastNames = lastNames,
                Emails = emails,
                Addresses1 = addresses1,
                States = states,
                Cities = cities,
                ZipCodes = zipCodes,
                Phones = phones,
                DatesAdded = datesAdded,
            },
            cancellationToken: cancellationToken
        );

        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }
}
