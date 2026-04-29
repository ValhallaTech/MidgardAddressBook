using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MidgardAddressBook.Core.Models;
using MidgardAddressBook.Core.Models.Pagination;

namespace MidgardAddressBook.Core.Interfaces;

/// <summary>
/// Persistence abstraction for <see cref="AddressBookEntry"/>.
/// </summary>
public interface IAddressBookEntryRepository
{
    /// <summary>Returns all entries ordered by last name, first name.</summary>
    Task<IReadOnlyList<AddressBookEntry>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns a single page of entries that match the supplied <paramref name="query"/>,
    /// together with the total number of matching records (for pagination controls).
    /// </summary>
    /// <param name="query">Pagination, sort, and filter parameters.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>
    /// A tuple containing the page of <see cref="AddressBookEntry"/> items and the total
    /// count of records matching the filter across all pages.
    /// </returns>
    Task<(IReadOnlyList<AddressBookEntry> Items, int TotalCount)> GetPagedAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a single entry or <c>null</c> if not found.</summary>
    Task<AddressBookEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new entry and returns its generated id.</summary>
    Task<int> CreateAsync(AddressBookEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entry. Returns <c>true</c> if a row was affected.</summary>
    Task<bool> UpdateAsync(AddressBookEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entry with the given id. Returns <c>true</c> if a row was affected.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
