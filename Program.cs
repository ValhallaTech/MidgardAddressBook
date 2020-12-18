using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MidgardAddressBook
{
    public static class Program
    {
        public static void Main( string[] args )
        {
            CreateHostBuilder( args ).Build( ).Run( );
        }

        private static IHostBuilder CreateHostBuilder( string[] args ) =>
            Host.CreateDefaultBuilder( args )
                .ConfigureWebHostDefaults( webBuilder =>
                                           {
                                               webBuilder.CaptureStartupErrors( true );
                                               webBuilder.UseSetting( WebHostDefaults.DetailedErrorsKey, "true" );
                                               webBuilder.UseStartup<Startup>( );
                                           } );
    }
}
