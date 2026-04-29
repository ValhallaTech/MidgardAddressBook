using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Core.Models;
using MidgardAddressBook.Core.Models.Pagination;
using MidgardAddressBook.DAL.Configuration;
using Npgsql;

namespace MidgardAddressBook.DAL.Repositories;

/// <summary>
/// Dapper + Npgsql implementation of <see cref="IAddressBookEntryRepository"/>.
/// </summary>
public class AddressBookEntryRepository : IAddressBookEntryRepository
{
    private readonly string _connectionString;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">Bound <see cref="DataOptions"/> providing the Postgres connection string.</param>
    public AddressBookEntryRepository(IOptions<DataOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = options.Value.PostgresConnectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    private const string SelectColumns =
        "id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email, "
        + "avatar AS Avatar, file_name AS FileName, address1 AS Address1, address2 AS Address2, "
        + "state AS State, city AS City, zip_code AS ZipCode, phone AS Phone, date_added AS DateAdded";

    // avatar and file_name intentionally omitted — list view only needs name, email, phone, city, state.
    private const string SelectColumnsForList =
        "id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email, "
        + "address1 AS Address1, address2 AS Address2, "
        + "state AS State, city AS City, zip_code AS ZipCode, phone AS Phone, date_added AS DateAdded";

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddressBookEntry>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(
            $"SELECT {SelectColumnsForList} FROM address_book_entries ORDER BY last_name, first_name",
            cancellationToken: cancellationToken
        );
        var rows = await connection.QueryAsync<AddressBookEntry>(command).ConfigureAwait(false);
        return [.. rows];
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AddressBookEntry> Items, int TotalCount)> GetPagedAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        // Resolve the sort column from the allow-list — never interpolate user input directly.
        string sortColumn = PagedQuery.AllowedSortFields.TryGetValue(query.SortField, out string? mappedColumn)
            ? mappedColumn
            : "last_name";

        string sortDirection = query.SortDirection == SortDirection.Descending ? "DESC" : "ASC";

        var parameters = new DynamicParameters();
        parameters.Add("Limit", query.PageSize);
        long offset = ((long)query.Page - 1L) * query.PageSize;
        parameters.Add("Offset", offset);

        string whereClause = string.Empty;
        if (!string.IsNullOrEmpty(query.SearchText))
        {
            whereClause =
                "WHERE (first_name ILIKE @Search OR last_name ILIKE @Search OR email ILIKE @Search OR phone ILIKE @Search)";
            parameters.Add("Search", $"%{query.SearchText}%");
        }

        // Both queries share the same WHERE clause; run them in a single round-trip via QueryMultipleAsync.
        string sql =
            $"""
            SELECT {SelectColumnsForList}
            FROM   address_book_entries
            {whereClause}
            ORDER  BY {sortColumn} {sortDirection}, id ASC
            LIMIT  @Limit OFFSET @Offset;

            SELECT COUNT(*)::int
            FROM   address_book_entries
            {whereClause};
            """;

        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command).ConfigureAwait(false);
        var rows = await multi.ReadAsync<AddressBookEntry>().ConfigureAwait(false);
        int totalCount = await multi.ReadSingleAsync<int>().ConfigureAwait(false);

        return ([.. rows], totalCount);
    }

    /// <inheritdoc />
    public async Task<AddressBookEntry?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(
            $"SELECT {SelectColumns} FROM address_book_entries WHERE id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken
        );
        return await connection
            .QuerySingleOrDefaultAsync<AddressBookEntry>(command)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        AddressBookEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var connection = CreateConnection();
        const string sql =
            @"
INSERT INTO address_book_entries
    (first_name, last_name, email, avatar, file_name, address1, address2, state, city, zip_code, phone, date_added)
VALUES
    (@FirstName, @LastName, @Email, @Avatar, @FileName, @Address1, @Address2, @State, @City, @ZipCode, @Phone, @DateAdded)
RETURNING id;";
        var command = new CommandDefinition(sql, entry, cancellationToken: cancellationToken);
        var id = await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);
        entry.Id = id;
        return id;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        AddressBookEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var connection = CreateConnection();
        const string sql =
            @"
UPDATE address_book_entries SET
    first_name = @FirstName,
    last_name  = @LastName,
    email      = @Email,
    avatar     = @Avatar,
    file_name  = @FileName,
    address1   = @Address1,
    address2   = @Address2,
    state      = @State,
    city       = @City,
    zip_code   = @ZipCode,
    phone      = @Phone
WHERE id = @Id;";
        var command = new CommandDefinition(sql, entry, cancellationToken: cancellationToken);
        var rows = await connection.ExecuteAsync(command).ConfigureAwait(false);
        return rows > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM address_book_entries WHERE id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken
        );
        var rows = await connection.ExecuteAsync(command).ConfigureAwait(false);
        return rows > 0;
    }
}
