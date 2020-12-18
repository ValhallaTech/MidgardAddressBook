//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using MidgardAddressBook.Data;
//using MidgardAddressBook.Models;

//namespace MidgardAddressBook.Helpers
//{
//    public static class SeederBreeder
//    {
//        public static void Initialize( IServiceProvider serviceProvider )
//        {
//            using AddressBookContext context = new AddressBookContext( serviceProvider
//                                                                           .GetRequiredService<
//                                                                               DbContextOptions<AddressBookContext>
//                                                                           >( ) );

//            if ( context.AddressBooks.Any( ) )
//            {
//                return;
//            }

//            static Task SeedDataAsync(
//                (AddressBookEntry, AddressBookEntry, AddressBookEntry, AddressBookEntry) addressBookEntry ) =>
//                SeedAddressEntry( addressBookEntry, context );

//            static Task SeedAddressEntry(
//                (AddressBookEntry, AddressBookEntry, AddressBookEntry, AddressBookEntry) addressBookEntry,
//                AddressBookContext                                                       context )
//            {
//                addressBookEntry = ( new AddressBookEntry( )
//                                     {
//                                         FirstName = "Fred",
//                                         LastName  = "Smith",
//                                         Email     = "testmail01@mailinator.com",
//                                         Address1  = "123 Main Street",
//                                         City      = "Winston Salem",
//                                         State     = "North Carolina",
//                                         ZipCode   = "27601",
//                                         Phone     = "555-555-6794",
//                                         DateAdded = DateTimeOffset.Now
//                                     },
//                                     new AddressBookEntry( )
//                                     {
//                                         FirstName = "Bill",
//                                         LastName  = "Williams",
//                                         Email     = "testmail02@mailinator.com",
//                                         Address1  = "456 Any Drive,",
//                                         City      = "Kernersville",
//                                         State     = "North Carolina",
//                                         ZipCode   = "27284",
//                                         Phone     = "555-555-8329",
//                                         DateAdded = DateTimeOffset.Now
//                                     },
//                                     new AddressBookEntry( )
//                                     {
//                                         FirstName = "Nugz",
//                                         LastName  = "McNuggetz",
//                                         Email     = "testmail03@mailinator.com",
//                                         Address1  = "789 Kumquat Court",
//                                         City      = "Charlotte",
//                                         State     = "North Carolina",
//                                         ZipCode   = "28205",
//                                         Phone     = "555-555-5800",
//                                         DateAdded = DateTimeOffset.Now
//                                     },
//                                     new AddressBookEntry( )
//                                     {
//                                         FirstName = "Kenny",
//                                         LastName  = "DuWitt",
//                                         Email     = "testmail04@mailinator.com",
//                                         Address1  = "6969 Average AVenue,",
//                                         City      = "Raleigh",
//                                         State     = "North Carolina",
//                                         ZipCode   = "27513",
//                                         Phone     = "555-555-0254",
//                                         DateAdded = DateTimeOffset.Now
//                                     } );
//                context.SaveChanges( );

//                return null;
//            }

//            return;
//        }
//    }
//}
