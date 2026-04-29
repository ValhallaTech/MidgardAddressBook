using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MidgardAddressBook.Core.Models.Pagination;
using MidgardAddressBook.DAL.Configuration;
using MidgardAddressBook.DAL.Extensions;
using MidgardAddressBook.DAL.Repositories;
using MidgardAddressBook.DAL.Tests.Integration;
using Xunit;

namespace MidgardAddressBook.DAL.Tests.Repositories;

/// <summary>
/// Integration tests that exercise <see cref="AddressBookEntryRepository.GetPagedAsync"/>
/// through its preferred <see cref="IDapper"/> code path. The repository is constructed
/// with both <see cref="IOptions{TOptions}"/> and an <see cref="IDapper"/> resolved from
/// the same DI container the application uses (via <see cref="ServiceCollectionExtensions.AddDal"/>),
/// pointed at the shared Testcontainers Postgres instance seeded by
/// <see cref="IntegrationCollectionFixture"/> with 1 000 deterministic rows.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class AddressBookEntryRepositoryPagedTests : IDisposable
{
    private const int SeedCount = 1000;

    private readonly ServiceProvider _provider;
    private readonly AddressBookEntryRepository _repository;

    public AddressBookEntryRepositoryPagedTests(IntegrationCollectionFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var services = new ServiceCollection();

        // Dapper.Extensions' PostgreSqlDapper resolves IConfiguration from the
        // service provider in its constructor (it would otherwise fall back to
        // ConnectionStrings:DefaultConnection). The application host has one
        // implicitly via WebApplicationBuilder; tests have to register one
        // explicitly. An empty in-memory IConfiguration is sufficient because
        // our DataOptionsConnectionStringProvider supplies the actual string.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        services.AddSingleton(configuration);
        services.AddLogging();

        services.Configure<DataOptions>(o =>
        {
            o.PostgresConnectionString = fixture.PostgresConnectionString;
            o.RedisConnectionString = fixture.RedisConnectionString;
        });
        services.AddDal();

        _provider = services.BuildServiceProvider();

        var options = _provider.GetRequiredService<IOptions<DataOptions>>();
        var dapper = _provider.GetRequiredService<IDapper>();

        _repository = new AddressBookEntryRepository(options, dapper);
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task Should_ReturnRequestedPage_When_PageGreaterThanOne()
    {
        // Arrange
        var page1Query = new PagedQuery(
            page: 1,
            pageSize: 10,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending);
        var page2Query = new PagedQuery(
            page: 2,
            pageSize: 10,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending);

        // Act
        var (page1Items, page1TotalCount) = await _repository.GetPagedAsync(page1Query);
        var (page2Items, page2TotalCount) = await _repository.GetPagedAsync(page2Query);

        // Assert — total count is stable across pages and the page-2 result set is
        // both the correct size and disjoint from page 1 (so OFFSET/skip is wired up
        // correctly and not silently returning the same page twice).
        page1TotalCount.Should().Be(SeedCount);
        page2TotalCount.Should().Be(SeedCount);
        page1Items.Should().HaveCount(10);
        page2Items.Should().HaveCount(10);
        page2Items.Select(e => e.Id).Should().NotIntersectWith(page1Items.Select(e => e.Id));
    }

    [Fact]
    public async Task Should_FilterRows_When_SearchTextProvided()
    {
        // Arrange — "user5@" matches exactly one seeded e-mail (user5@example.com);
        // "user50@" / "user500@" do not match because the trailing "@" anchors it.
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: "user5@",
            sortField: "LastName",
            sortDirection: SortDirection.Ascending);

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Email.Should().Be("user5@example.com");
    }

    [Fact]
    public async Task Should_OrderResultsDescending_When_SortDirectionDescending()
    {
        // Arrange — descending sort by Email pulls the largest e-mails to the
        // top. Exact ordering depends on the PostgreSQL collation, so the test
        // asserts the contract (descending order, requested page size) rather
        // than a specific row.
        var query = new PagedQuery(
            page: 1,
            pageSize: 5,
            searchText: null,
            sortField: "Email",
            sortDirection: SortDirection.Descending);

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(SeedCount);
        items.Should().HaveCount(5);
        items.Should().BeInDescendingOrder(e => e.Email);
    }

    [Fact]
    public async Task Should_FallBackToLastName_When_SortFieldUnknown()
    {
        // Arrange — pass a sort field that is not in PagedQuery.AllowedSortFields.
        // PagedQuery's constructor normalises it to the default "LastName"; the
        // repository's own TryGetValue check is defence-in-depth that maps to the
        // last_name column. Either way the query must succeed and return all rows.
        var query = new PagedQuery(
            page: 1,
            pageSize: 25,
            searchText: null,
            sortField: "NotARealField",
            sortDirection: SortDirection.Ascending);

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(query);

        // Assert
        query.SortField.Should().Be("LastName"); // sanity: PagedQuery normalised it
        totalCount.Should().Be(SeedCount);
        items.Should().HaveCount(25);
    }

    [Fact]
    public async Task Should_ResolveSortField_When_CasingDiffersFromAllowList()
    {
        // Arrange — "lastname" is allowed because PagedQuery.AllowedSortFields
        // uses StringComparer.OrdinalIgnoreCase. The repository's own TryGetValue
        // resolves it against the same ignore-case dictionary so it must map to
        // the last_name column without falling through to the default branch.
        var lower = new PagedQuery(
            page: 1,
            pageSize: 5,
            searchText: null,
            sortField: "lastname",
            sortDirection: SortDirection.Ascending);

        var canonical = new PagedQuery(
            page: 1,
            pageSize: 5,
            searchText: null,
            sortField: "LastName",
            sortDirection: SortDirection.Ascending);

        // Act
        var (lowerItems, lowerTotal) = await _repository.GetPagedAsync(lower);
        var (canonicalItems, canonicalTotal) = await _repository.GetPagedAsync(canonical);

        // Assert — case-insensitive resolution must yield the same totals and
        // exactly the same row order as the canonical casing.
        lowerTotal.Should().Be(SeedCount);
        canonicalTotal.Should().Be(SeedCount);
        lowerItems.Should().BeEquivalentTo(canonicalItems, options => options.WithStrictOrdering());
    }
}
