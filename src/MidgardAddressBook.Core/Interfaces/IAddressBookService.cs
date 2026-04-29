using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Models.Pagination;

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

    /// <summary>
    /// Returns a single page of entries matching the supplied <paramref name="query"/>,
    /// with total count metadata for pagination controls.
    /// Results are not cached due to the high cardinality of possible permutations.
    /// </summary>
    /// <param name="query">Pagination, sort, and filter parameters.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the page items and paging metadata.</returns>
    Task<PagedResult<AddressBookEntryDto>> GetPagedAsync(
        PagedQuery query,
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
