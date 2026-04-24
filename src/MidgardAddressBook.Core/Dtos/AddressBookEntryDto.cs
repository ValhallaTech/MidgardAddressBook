using System;
using System.ComponentModel.DataAnnotations;

namespace MidgardAddressBook.Core.Dtos;

/// <summary>
/// Data transfer object for <see cref="Models.AddressBookEntry"/> used by the Blazor UI layer.
/// </summary>
public class AddressBookEntryDto
{
    /// <summary>Gets or sets the unique identifier. Zero for new records.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the first name.</summary>
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the e-mail address.</summary>
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets address line 1.</summary>
    [Required, StringLength(200)]
    public string Address1 { get; set; } = string.Empty;

    /// <summary>Gets or sets optional address line 2.</summary>
    [StringLength(200)]
    public string? Address2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the state/province.</summary>
    [Required, StringLength(100)]
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    [Required, StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number.</summary>
    [Required, StringLength(40)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp the entry was created.</summary>
    public DateTimeOffset DateAdded { get; set; }
}
