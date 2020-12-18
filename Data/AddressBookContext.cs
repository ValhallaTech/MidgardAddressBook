using Microsoft.EntityFrameworkCore;
using MidgardAddressBook.Models;

namespace MidgardAddressBook.Data
{
    public class AddressBookContext : DbContext
    {
        public AddressBookContext( DbContextOptions<AddressBookContext> options ) : base( options )
        {
        }

        public DbSet<AddressBookEntry> AddressBooks { get; set; }
    }
}
