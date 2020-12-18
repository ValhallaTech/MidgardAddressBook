using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidgardAddressBook.Data;
using MidgardAddressBook.Models;

namespace MidgardAddressBook.Controllers
{
    public class AddressBookEntriesController : Controller
    {
        private readonly AddressBookContext context;

        public AddressBookEntriesController( AddressBookContext context ) => this.context = context;

        // GET: AddressBookEntries
        public async Task<IActionResult> Index( ) => this.View( await this.context.AddressBooks.ToListAsync( ) );

        // GET: AddressBookEntries/Details/5
        public async Task<IActionResult> Details( int? id )
        {
            if ( id == null )
            {
                return this.NotFound( );
            }

            AddressBookEntry addressBookEntry = await this.context.AddressBooks
                                                          .FirstOrDefaultAsync( m => m.Id == id );

            if ( addressBookEntry == null )
            {
                return this.NotFound( );
            }

            return this.View( addressBookEntry );
        }

        // GET: AddressBookEntries/Create
        public IActionResult Create( ) => this.View( );

        // POST: AddressBookEntries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( [Bind( "Id,FirstName,LastName,Email,Avatar,FileName,Adress1,Address2,City,ZipCode,Phone,DateAdded" )]
                                                 AddressBookEntry addressBookEntry )
        {
            if ( !this.ModelState.IsValid ) return this.View( addressBookEntry );

            this.context.Add( addressBookEntry );
            await this.context.SaveChangesAsync( );

            return this.RedirectToAction( nameof( this.Index ) );
        }

        // GET: AddressBookEntries/Edit/5
        public async Task<IActionResult> Edit( int? id )
        {
            if ( id == null )
            {
                return this.NotFound( );
            }

            AddressBookEntry addressBookEntry = await this.context.AddressBooks.FindAsync( id );

            if ( addressBookEntry == null )
            {
                return this.NotFound( );
            }

            return this.View( addressBookEntry );
        }

        // POST: AddressBookEntries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit( int id,
                                               [Bind( "Id,FirstName,LastName,Email,Avatar,FileName,Adress1,Address2,City,ZipCode,Phone,DateAdded" )]
                                               AddressBookEntry addressBookEntry )
        {
            if ( id != addressBookEntry.Id )
            {
                return this.NotFound( );
            }

            if ( this.ModelState.IsValid )
            {
                try
                {
                    this.context.Update( addressBookEntry );
                    await this.context.SaveChangesAsync( );
                }
                catch ( DbUpdateConcurrencyException )
                {
                    if ( !this.AddressBookEntryExists( addressBookEntry.Id ) )
                    {
                        return this.NotFound( );
                    }
                    else
                    {
                        throw;
                    }
                }

                return this.RedirectToAction( nameof( this.Index ) );
            }

            return this.View( addressBookEntry );
        }

        // GET: AddressBookEntries/Delete/5
        public async Task<IActionResult> Delete( int? id )
        {
            if ( id == null )
            {
                return this.NotFound( );
            }

            AddressBookEntry addressBookEntry = await this.context.AddressBooks
                                                          .FirstOrDefaultAsync( m => m.Id == id );

            if ( addressBookEntry == null )
            {
                return this.NotFound( );
            }

            return this.View( addressBookEntry );
        }

        // POST: AddressBookEntries/Delete/5
        [HttpPost]
        [ActionName( "Delete" )]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed( int id )
        {
            AddressBookEntry addressBookEntry = await this.context.AddressBooks.FindAsync( id );
            this.context.AddressBooks.Remove( addressBookEntry );
            await this.context.SaveChangesAsync( );

            return this.RedirectToAction( nameof( this.Index ) );
        }

        private bool AddressBookEntryExists( int id )
        {
            return this.context.AddressBooks.Any( e => e.Id == id );
        }
    }
}
