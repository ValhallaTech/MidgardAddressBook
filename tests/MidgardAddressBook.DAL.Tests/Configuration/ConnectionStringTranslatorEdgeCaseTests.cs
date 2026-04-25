using FluentAssertions;
using MidgardAddressBook.DAL.Configuration;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Configuration;

/// <summary>
/// Additional <see cref="ConnectionStringTranslator"/> coverage: postgres/redis URL
/// edge cases (no password, no port, sslmode variations, percent-encoded creds, etc.).
/// </summary>
public class ConnectionStringTranslatorEdgeCaseTests
{
    [Theory]
    [InlineData("postgres://localhost/db", "Host=localhost", "Port=5432", "Database=db")]
    [InlineData(
        "postgresql://10.0.0.1:5433/midgard",
        "Host=10.0.0.1",
        "Port=5433",
        "Database=midgard"
    )]
    public void ToNpgsqlConnectionString_HandlesMissingPassword(
        string url,
        string expectedHost,
        string expectedPort,
        string expectedDb
    )
    {
        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(url);

        result.Should().NotBeNull();
        result!.Should().Contain(expectedHost);
        result.Should().Contain(expectedPort);
        result.Should().Contain(expectedDb);
    }

    [Theory]
    [InlineData("postgres://u:p@h/db?sslmode=require", "SSL Mode=Require")]
    [InlineData("postgres://u:p@h/db?sslmode=disable", "SSL Mode=Disable")]
    [InlineData("postgres://u:p@h/db?sslmode=allow", "SSL Mode=Allow")]
    [InlineData("postgres://u:p@h/db?sslmode=verify-ca", "SSL Mode=VerifyCA")]
    [InlineData("postgres://u:p@h/db?sslmode=verify-full", "SSL Mode=VerifyFull")]
    [InlineData("postgres://u:p@h/db?sslmode=unknown-mode", "SSL Mode=Prefer")]
    [InlineData("postgres://u:p@h/db", "SSL Mode=Prefer")]
    public void ToNpgsqlConnectionString_MapsSslModeFromQueryString(string url, string expected)
    {
        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(url);

        result.Should().NotBeNull().And.Contain(expected);
    }

    [Fact]
    public void ToNpgsqlConnectionString_DecodesPercentEncodedCredentials()
    {
        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(
            "postgres://us%40er:p%40ss@host/db"
        );

        result.Should().NotBeNull();
        result!.Should().Contain("Username=us@er");
        // Npgsql connection-string builder may quote the password; either form is fine.
        result.Should().MatchRegex("Password=('p@ss'|p@ss)");
    }

    [Fact]
    public void ToNpgsqlConnectionString_IgnoresInvalidSslRootCertPath()
    {
        // sslrootcert that does not exist on disk should not be assigned (file existence guarded).
        var result = ConnectionStringTranslator.ToNpgsqlConnectionString(
            "postgres://u:p@h/db?sslmode=require&sslrootcert=/nonexistent/path/ca.pem"
        );

        result.Should().NotBeNull();
        result!.Should().NotContain("Root Certificate");
        result.Should().NotContain("/nonexistent/path/ca.pem");
    }

    [Fact]
    public void ToNpgsqlConnectionString_DoesNotThrow_OnMalformedUrl()
    {
        // Contract under test: the translator must never throw for a malformed
        // scheme-prefixed URL — it returns either null or a sanitized connection
        // string. System.Uri tolerates many oddities (e.g. "postgres://%"), so we
        // only assert non-throwing behavior and that no raw '%' leaks through.
        var act = () => ConnectionStringTranslator.ToNpgsqlConnectionString("postgres://%");

        var result = act.Should().NotThrow().Subject;
        (result ?? string.Empty).Should().NotContain("%");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToRedisConfiguration_ReturnsNull_ForNullOrWhitespace(string? value)
    {
        ConnectionStringTranslator.ToRedisConfiguration(value).Should().BeNull();
    }

    [Fact]
    public void ToRedisConfiguration_TranslatesRedisUrl_DefaultPort_NoPassword()
    {
        var result = ConnectionStringTranslator.ToRedisConfiguration("redis://redis-host");

        result.Should().Be("redis-host:6379");
    }

    [Fact]
    public void ToRedisConfiguration_TranslatesRedisUrl_PasswordOnlyUserInfo()
    {
        // userinfo "secret" with no colon is interpreted as the password.
        var result = ConnectionStringTranslator.ToRedisConfiguration(
            "redis://secret@redis-host:6380"
        );

        result.Should().Contain("redis-host:6380");
        result!.Should().Contain("password=secret");
        result.Should().NotContain("ssl=true");
    }

    [Fact]
    public void ToRedisConfiguration_DecodesPercentEncodedPassword()
    {
        var result = ConnectionStringTranslator.ToRedisConfiguration(
            "rediss://:p%40ss%21@example.com:6380"
        );

        result.Should().Contain("password=p@ss!");
        result!.Should().Contain("ssl=true");
    }

    [Fact]
    public void ToRedisConfiguration_OmitsPasswordClause_WhenUserInfoIsEmpty()
    {
        var result = ConnectionStringTranslator.ToRedisConfiguration("redis://example.com:6379");

        result.Should().Be("example.com:6379");
        result.Should().NotContain("password=");
    }

    [Fact]
    public void ToRedisConfiguration_AddsSslFlag_ForRedissScheme()
    {
        var result = ConnectionStringTranslator.ToRedisConfiguration("rediss://example.com");

        result.Should().Be("example.com:6379,ssl=true");
    }
}
