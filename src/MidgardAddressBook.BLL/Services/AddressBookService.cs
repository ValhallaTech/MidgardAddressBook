using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MidgardAddressBook.Core.Caching;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.Core.Models;
using MidgardAddressBook.Core.Models.Pagination;

namespace MidgardAddressBook.BLL.Services;

/// <summary>
/// Default <see cref="IAddressBookService"/> implementation that coordinates the repository and
/// cache. Lists are cached with a short TTL and invalidated on writes.
/// </summary>
public class AddressBookService : IAddressBookService
{
    private const string ListCacheKey = CacheKeys.AddressBookList;
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAddressBookEntryRepository _repository;
    private readonly ICacheService _cache;
    private readonly IMapper _mapper;
    private readonly ILogger<AddressBookService> _logger;

    /// <summary>Initializes a new instance.</summary>
    public AddressBookService(
        IAddressBookEntryRepository repository,
        ICacheService cache,
        IMapper mapper,
        ILogger<AddressBookService> logger
    )
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddressBookEntryDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var cached = await _cache
            .GetAsync<CachedList>(ListCacheKey, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            _logger.LogDebug(
                "AddressBook list served from cache ({Count} rows).",
                cached.Items.Count
            );
            return cached.Items;
        }

        var entries = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var dtos = _mapper.Map<IReadOnlyList<AddressBookEntryDto>>(entries);
        await _cache
            .SetAsync(ListCacheKey, new CachedList(dtos), ListCacheTtl, cancellationToken)
            .ConfigureAwait(false);
        return dtos;
    }

    /// <inheritdoc />
    public async Task<PagedResult<AddressBookEntryDto>> GetPagedAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var (items, totalCount) = await _repository
            .GetPagedAsync(sanitized, cancellationToken)
            .ConfigureAwait(false);

        var dtos = _mapper.Map<IReadOnlyList<AddressBookEntryDto>>(items);

        PagedResult<AddressBookEntryDto> result = new()
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = sanitized.Page,
            PageSize = sanitized.PageSize,
        };

        _logger.LogDebug(
            "GetPagedAsync returned page {Page}/{TotalPages} ({PageSize} items per page, {TotalCount} total).",
            result.Page,
            result.TotalPages,
            result.PageSize,
            result.TotalCount
        );

        return result;
    }

    /// <inheritdoc />
    public async Task<AddressBookEntryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return entry is null ? null : _mapper.Map<AddressBookEntryDto>(entry);
    }

    /// <inheritdoc />
    public async Task<AddressBookEntryDto> CreateAsync(
        AddressBookEntryDto dto,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dto);
        var entry = _mapper.Map<AddressBookEntry>(dto);
        entry.DateAdded = DateTimeOffset.UtcNow;
        await _repository.CreateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(ListCacheKey, cancellationToken).ConfigureAwait(false);
        return _mapper.Map<AddressBookEntryDto>(entry);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        AddressBookEntryDto dto,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dto);
        var existing = await _repository
            .GetByIdAsync(dto.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        _mapper.Map(dto, existing);
        var updated = await _repository
            .UpdateAsync(existing, cancellationToken)
            .ConfigureAwait(false);
        if (updated)
        {
            await _cache.RemoveAsync(ListCacheKey, cancellationToken).ConfigureAwait(false);
        }

        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            await _cache.RemoveAsync(ListCacheKey, cancellationToken).ConfigureAwait(false);
        }

        return deleted;
    }

    /// <summary>Wrapper so lists can be serialized as a single cached JSON object.</summary>
    private sealed record CachedList(IReadOnlyList<AddressBookEntryDto> Items);
}
