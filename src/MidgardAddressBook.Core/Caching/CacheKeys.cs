namespace MidgardAddressBook.Core.Caching;

/// <summary>
/// Well-known Redis cache key constants shared across the application layers.
/// Centralizing these strings here prevents key mismatches when multiple projects
/// need to reference the same cache entries (e.g. DAL seeding invalidating a key
/// written by BLL read-through logic).
/// </summary>
public static class CacheKeys
{
    /// <summary>Caches the full, unfiltered address-book entry list.</summary>
    public const string AddressBookList = "address-book:entries:all";
}
