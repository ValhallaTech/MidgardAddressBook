using FluentMigrator;

namespace MidgardAddressBook.DAL.Migrations;

/// <summary>
/// Replaces the narrow <c>ix_address_book_entries_last_name</c> index with a composite
/// <c>(last_name ASC, first_name ASC)</c> index that better supports name-based searches, and
/// adds a <c>UNIQUE</c> constraint on <c>email</c> to enforce address-book uniqueness at the
/// database level.
/// </summary>
[Migration(202404270001, "Optimise address_book_entries indexes")]
public class M202404270001_OptimiseAddressBookIndexes : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        // 1. Remove the previous narrow index so the composite one can take its place.
        Delete.Index("ix_address_book_entries_last_name").OnTable("address_book_entries");

        // 2. Create the composite index covering (last_name, first_name) to support
        //    surname-first searches and ORDER BY last_name, first_name efficiently.
        Create
            .Index("ix_address_book_entries_last_name_first_name")
            .OnTable("address_book_entries")
            .OnColumn("last_name")
            .Ascending()
            .OnColumn("first_name")
            .Ascending();

        // 3. Add a unique constraint on email — enforces one entry per e-mail address.
        //    Execute.Sql is used here because FluentMigrator's Create.UniqueConstraint
        //    does not expose IF NOT EXISTS semantics and the raw DDL is straightforward.
        Execute.Sql(
            "ALTER TABLE address_book_entries ADD CONSTRAINT uq_address_book_entries_email UNIQUE (email);"
        );
    }

    /// <inheritdoc />
    public override void Down()
    {
        // 1. Remove the unique constraint added in Up().
        Execute.Sql(
            "ALTER TABLE address_book_entries DROP CONSTRAINT IF EXISTS uq_address_book_entries_email;"
        );

        // 2. Remove the composite index added in Up().
        Delete
            .Index("ix_address_book_entries_last_name_first_name")
            .OnTable("address_book_entries");

        // 3. Restore the original narrow last_name index to return to the prior state.
        Create
            .Index("ix_address_book_entries_last_name")
            .OnTable("address_book_entries")
            .OnColumn("last_name")
            .Ascending();
    }
}
