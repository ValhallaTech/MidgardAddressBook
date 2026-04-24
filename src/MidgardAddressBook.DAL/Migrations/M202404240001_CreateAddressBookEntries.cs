using FluentMigrator;

namespace MidgardAddressBook.DAL.Migrations;

/// <summary>
/// Creates the <c>address_book_entries</c> table.
/// </summary>
[Migration(202404240001, "Create address_book_entries")]
public class M202404240001_CreateAddressBookEntries : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Create.Table("address_book_entries")
            .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("email").AsString(256).NotNullable()
            .WithColumn("avatar").AsCustom("bytea").Nullable()
            .WithColumn("file_name").AsString(256).Nullable()
            .WithColumn("address1").AsString(200).NotNullable()
            .WithColumn("address2").AsString(200).Nullable()
            .WithColumn("state").AsString(100).NotNullable()
            .WithColumn("city").AsString(100).NotNullable()
            .WithColumn("zip_code").AsString(20).NotNullable()
            .WithColumn("phone").AsString(40).NotNullable()
            .WithColumn("date_added").AsCustom("timestamptz").NotNullable();

        Create.Index("ix_address_book_entries_last_name")
            .OnTable("address_book_entries")
            .OnColumn("last_name").Ascending();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("address_book_entries");
    }
}
