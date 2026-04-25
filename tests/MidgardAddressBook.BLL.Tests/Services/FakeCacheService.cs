using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MidgardAddressBook.Core.Interfaces;

namespace MidgardAddressBook.BLL.Tests.Services;

/// <summary>
/// Hand-rolled in-memory <see cref="ICacheService"/> fake. Used in lieu of Moq for cache
/// tests because <see cref="AddressBookService"/> serializes lists into a private nested
/// record (<c>CachedList</c>) that is not constructable from the test assembly. Storing the
/// value as <see cref="object"/> and casting via the open generic on the way out preserves
/// the round-trip behaviour without coupling tests to private types.
/// </summary>
internal sealed class FakeCacheService : ICacheService
{
    public Dictionary<string, object> Storage { get; } = new(StringComparer.Ordinal);
    public List<string> RemovedKeys { get; } = new();
    public List<(string Key, object Value, TimeSpan? Ttl)> Sets { get; } = new();
    public List<CancellationToken> ObservedTokens { get; } = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        ObservedTokens.Add(cancellationToken);
        return Task.FromResult(Storage.TryGetValue(key, out var value) ? (T?)value : null);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ObservedTokens.Add(cancellationToken);
        Storage[key] = value!;
        Sets.Add((key, value!, ttl));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ObservedTokens.Add(cancellationToken);
        RemovedKeys.Add(key);
        Storage.Remove(key);
        return Task.CompletedTask;
    }
}
