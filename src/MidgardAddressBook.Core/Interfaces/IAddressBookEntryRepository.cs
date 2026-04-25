using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MidgardAddressBook.Core.Models;

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

    /// <summary>Returns a single entry or <c>null</c> if not found.</summary>
    Task<AddressBookEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new entry and returns its generated id.</summary>
    Task<int> CreateAsync(AddressBookEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entry. Returns <c>true</c> if a row was affected.</summary>
    Task<bool> UpdateAsync(AddressBookEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entry with the given id. Returns <c>true</c> if a row was affected.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
