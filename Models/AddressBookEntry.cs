using System;

namespace MidgardAddressBook.Models
{
    public class AddressBookEntry
    {
        public int      Id        { get; set; }
        public string   FirstName { get; set; }
        public string   LastName  { get; set; }
        public string   Email     { get; set; }
        public byte[]   Avatar    { get; set; }
        public string   FileName  { get; set; }
        public string   Address1  { get; set; }
        public string   Address2  { get; set; }
        public string   State     { get; set; }
        public string   City      { get; set; }
        public string   ZipCode   { get; set; }
        public string   Phone     { get; set; }
        public DateTimeOffset DateAdded { get; set; }
    }
}
