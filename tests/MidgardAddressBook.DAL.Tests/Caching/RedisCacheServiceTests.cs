using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MidgardAddressBook.DAL.Caching;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Caching;

/// <summary>
/// Unit tests for <see cref="RedisCacheService"/> using a mocked <see cref="IConnectionMultiplexer"/>
/// and <see cref="IDatabase"/>. Verifies serialization round-trip, null-on-miss, and graceful
/// degradation on Redis/JSON failures.
/// </summary>
public class RedisCacheServiceTests
{
    private sealed record Sample(int Id, string Name);

    private static (RedisCacheService Sut, Mock<IDatabase> Db) CreateSut()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        var db = new Mock<IDatabase>(MockBehavior.Strict);
        multiplexer
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db.Object);
        var sut = new RedisCacheService(multiplexer.Object, NullLogger<RedisCacheService>.Instance);
        return (sut, db);
    }

    [Fact]
    public void Constructor_Throws_WhenMultiplexerIsNull()
    {
        Action act = () => _ = new RedisCacheService(null!, NullLogger<RedisCacheService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("multiplexer");
    }

    [Fact]
    public void Constructor_Throws_WhenLoggerIsNull()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>().Object;
        Action act = () => _ = new RedisCacheService(multiplexer, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ---- GetAsync ----------------------------------------------------------

    [Fact]
    public async Task GetAsync_ReturnsNull_OnCacheMiss()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.StringGetAsync("missing", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await sut.GetAsync<Sample>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_OnEmptyValue()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.StringGetAsync("empty", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.EmptyString);

        var result = await sut.GetAsync<Sample>("empty");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_DeserializesValueOnHit()
    {
        var (sut, db) = CreateSut();
        var payload = JsonSerializer.Serialize(
            new Sample(1, "midgard"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        db.Setup(d => d.StringGetAsync("hit", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)payload);

        var result = await sut.GetAsync<Sample>("hit");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("midgard");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_OnRedisException()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.StringGetAsync("boom", It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "x"));

        var result = await sut.GetAsync<Sample>("boom");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_OnInvalidJson()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.StringGetAsync("bad", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"this is not json");

        var result = await sut.GetAsync<Sample>("bad");

        result.Should().BeNull();
    }

    // ---- SetAsync ----------------------------------------------------------

    [Fact]
    public async Task SetAsync_SerializesValue_AndForwardsTtlAndAlwaysFlag()
    {
        var (sut, db) = CreateSut();
        RedisValue captured = default;
        TimeSpan? capturedTtl = null;
        When capturedWhen = When.NotExists;

        void Capture(RedisValue v, TimeSpan? ttl, When w)
        {
            captured = v;
            capturedTtl = ttl;
            capturedWhen = w;
        }

        // Cover both the 4-arg overload and the 5-arg (CommandFlags) overload so
        // the test remains valid regardless of which overload the SUT resolves to.
        db.Setup(d =>
                d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>()
                )
            )
            .Callback<RedisKey, RedisValue, TimeSpan?, When>((_, v, ttl, w) => Capture(v, ttl, w))
            .ReturnsAsync(true);
        db.Setup(d =>
                d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()
                )
            )
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>(
                (_, v, ttl, w, _) => Capture(v, ttl, w)
            )
            .ReturnsAsync(true);

        await sut.SetAsync("k", new Sample(7, "Tyr"), TimeSpan.FromSeconds(30));

        capturedTtl.Should().Be(TimeSpan.FromSeconds(30));
        capturedWhen.Should().Be(When.Always);
        var json = (string)captured!;
        json.Should().Contain("\"id\":7").And.Contain("\"name\":\"Tyr\"");
    }

    [Fact]
    public async Task SetAsync_SwallowsRedisException()
    {
        var (sut, db) = CreateSut();
        db.Setup(d =>
                d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>()
                )
            )
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "x"));
        db.Setup(d =>
                d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()
                )
            )
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "x"));

        var act = async () => await sut.SetAsync("k", new Sample(1, "x"));

        await act.Should().NotThrowAsync();
    }

    // ---- RemoveAsync -------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_CallsKeyDeleteAsync()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.KeyDeleteAsync("k", It.IsAny<CommandFlags>())).ReturnsAsync(true);

        await sut.RemoveAsync("k");

        db.Verify(d => d.KeyDeleteAsync("k", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_SwallowsRedisException()
    {
        var (sut, db) = CreateSut();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "x"));

        var act = async () => await sut.RemoveAsync("k");

        await act.Should().NotThrowAsync();
    }
}
