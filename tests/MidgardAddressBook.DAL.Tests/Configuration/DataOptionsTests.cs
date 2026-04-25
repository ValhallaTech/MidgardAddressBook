using FluentAssertions;
using MidgardAddressBook.DAL.Configuration;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Configuration;

/// <summary>
/// Tests for the <see cref="DataOptions"/> POCO bound from configuration.
/// </summary>
public class DataOptionsTests
{
    [Fact]
    public void Defaults_AreEmptyStrings()
    {
        var opts = new DataOptions();

        opts.PostgresConnectionString.Should().BeEmpty();
        opts.RedisConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var opts = new DataOptions
        {
            PostgresConnectionString = "Host=h;Username=u;Password=p;Database=d",
            RedisConnectionString = "h:6379",
        };

        opts.PostgresConnectionString.Should().Contain("Host=h");
        opts.RedisConnectionString.Should().Be("h:6379");
    }

    [Fact]
    public void SectionName_IsStable()
    {
        DataOptions.SectionName.Should().Be("Data");
    }
}
