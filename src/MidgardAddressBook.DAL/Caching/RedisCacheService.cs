using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MidgardAddressBook.Core.Interfaces;
using StackExchange.Redis;

namespace MidgardAddressBook.DAL.Caching;

/// <summary>
/// StackExchange.Redis-backed implementation of <see cref="ICacheService"/>.
/// Cache misses and transient failures degrade gracefully (return null / swallow).
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web
    );
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    /// <summary>Initializes a new instance.</summary>
    public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IDatabase GetDatabase() => _multiplexer.GetDatabase();

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var value = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}; returning null.", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            await GetDatabase()
                .StringSetAsync(key, payload, ttl, When.Always)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            _logger.LogWarning(
                ex,
                "Redis SET failed for key {CacheKey}; continuing without caching.",
                key
            );
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetDatabase().KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DEL failed for key {CacheKey}.", key);
        }
    }
}
