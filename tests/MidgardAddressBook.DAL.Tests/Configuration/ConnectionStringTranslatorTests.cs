using FluentAssertions;
using MidgardAddressBook.DAL.Configuration;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Configuration;

/// <summary>
/// Smoke tests for <see cref="ConnectionStringTranslator"/>.
/// </summary>
public class ConnectionStringTranslatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToNpgsqlConnectionString_ReturnsNull_ForNullOrWhitespace(string? value)
    {
        ConnectionStringTranslator.ToNpgsqlConnectionString(value).Should().BeNull();
    }

    [Fact]
    public void ToNpgsqlConnectionString_PassesThroughKeyValueString()
    {
        var input = "Host=localhost;Database=db;Username=u;Password=p";

        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(input);

        result.Should().Be(input);
    }

    [Fact]
    public void ToNpgsqlConnectionString_TranslatesPostgresUrl()
    {
        var url = "postgres://alice:s3cret@db.example.com:6543/midgard?sslmode=require";

        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(url);

        result.Should().NotBeNull();
        result!.Should().Contain("Host=db.example.com");
        result.Should().Contain("Port=6543");
        result.Should().Contain("Username=alice");
        result.Should().Contain("Database=midgard");
        result.Should().Contain("SSL Mode=Require");
    }

    [Fact]
    public void ToRedisConfiguration_TranslatesRedissUrlWithPassword()
    {
        var url = "rediss://:p%40ss@redis.example.com:6380";

        var result = ConnectionStringTranslator.ToRedisConfiguration(url);

        result.Should().NotBeNull();
        result!.Should().Contain("redis.example.com:6380");
        result.Should().Contain("password=p@ss");
        result.Should().Contain("ssl=true");
    }

    [Fact]
    public void ToRedisConfiguration_PassesThroughHostPortConfig()
    {
        const string config = "redis-host:6379";

        var result = ConnectionStringTranslator.ToRedisConfiguration(config);

        result.Should().Be(config);
    }
}
