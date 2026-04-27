using FluentMigrator;

namespace MidgardAddressBook.DAL.Migrations;

/// <summary>
/// Inserts 10 deterministic synthetic rows into <c>address_book_entries</c> for CI and
/// smoke-test verification.
/// </summary>
/// <remarks>
/// Tagged <c>Seed</c> so this migration is skipped during normal schema-only runs and only
/// applied when the FluentMigrator runner is explicitly configured with the <c>Seed</c> tag
/// (e.g. in a dedicated seeding step of the test pipeline). For bulk pre-population of
/// performance-test data sets, use
/// <see cref="MidgardAddressBook.DAL.Seeding.DatabaseSeeder"/> directly instead.
/// </remarks>
[Tags("Seed")]
[Migration(202404240002, "Seed performance data")]
public class M202404240002_SeedPerformanceData : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Execute.Sql(UpSql);
    }

    /// <inheritdoc />
    public override void Down()
    {
        // Removes all rows whose email matches the seeded pattern, which covers both this
        // migration and any rows inserted by DatabaseSeeder using the same convention.
        Execute.Sql(DownSql);
    }

    // Hard-coded constant — no user input involved, so no injection risk.
    private const string DownSql =
        "DELETE FROM address_book_entries WHERE email LIKE 'user%@example.com';";

    // Ten hard-coded rows with a fixed UTC anchor date so the timestamps are fully
    // deterministic regardless of when the migration is applied. The anchor matches the
    // migration's own date (2024-04-24) and offsets mirror the DatabaseSeeder convention
    // (row i → anchor − i days), making integration-test assertions predictable.
    private const string UpSql = """
        INSERT INTO address_book_entries
            (first_name, last_name, email, address1, address2, state, city, zip_code, phone, date_added)
        VALUES
            ('First0', 'Last0', 'user0@example.com', '0 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000000', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '0 days'),
            ('First1', 'Last1', 'user1@example.com', '1 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000001', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '1 days'),
            ('First2', 'Last2', 'user2@example.com', '2 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000002', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '2 days'),
            ('First3', 'Last3', 'user3@example.com', '3 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000003', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '3 days'),
            ('First4', 'Last4', 'user4@example.com', '4 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000004', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '4 days'),
            ('First5', 'Last5', 'user5@example.com', '5 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000005', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '5 days'),
            ('First6', 'Last6', 'user6@example.com', '6 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000006', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '6 days'),
            ('First7', 'Last7', 'user7@example.com', '7 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000007', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '7 days'),
            ('First8', 'Last8', 'user8@example.com', '8 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000008', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '8 days'),
            ('First9', 'Last9', 'user9@example.com', '9 Test Street', NULL, 'NY', 'Midgard', '10001', '555-0000009', '2024-04-24 00:00:00+00'::timestamptz - INTERVAL '9 days');
        """;
}
