using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MidgardAddressBook.Core.Dtos;

namespace MidgardAddressBook.Core.Interfaces;

/// <summary>
/// Application-facing CRUD service for address book entries, exposing DTOs to the presentation layer.
/// </summary>
public interface IAddressBookService
{
    /// <summary>Lists all entries (may be served from cache).</summary>
    Task<IReadOnlyList<AddressBookEntryDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Loads a single entry.</summary>
    Task<AddressBookEntryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new entry and returns the persisted DTO (with generated id/DateAdded).</summary>
    Task<AddressBookEntryDto> CreateAsync(
        AddressBookEntryDto dto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an existing entry.</summary>
    Task<bool> UpdateAsync(AddressBookEntryDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entry by id.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
