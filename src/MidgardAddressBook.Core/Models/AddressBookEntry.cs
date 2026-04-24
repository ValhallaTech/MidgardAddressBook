using System;

namespace MidgardAddressBook.Core.Models;

/// <summary>
/// Domain entity representing an address book contact.
/// </summary>
public class AddressBookEntry
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the contact's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the contact's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the contact's e-mail address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the avatar image bytes, if any.</summary>
    public byte[]? Avatar { get; set; }

    /// <summary>Gets or sets the avatar file name, if any.</summary>
    public string? FileName { get; set; }

    /// <summary>Gets or sets address line 1.</summary>
    public string Address1 { get; set; } = string.Empty;

    /// <summary>Gets or sets address line 2.</summary>
    public string? Address2 { get; set; }

    /// <summary>Gets or sets the state/province.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the city/town.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal/zip code.</summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC offset timestamp when the entry was created.</summary>
    public DateTimeOffset DateAdded { get; set; }
}
