using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                // Create a new Scope so that we can request Services directly from the DI container.
                // This is just to demonstrate a sample request.
                // You would remove this whole using block from a real application.
                using (var scope = host.Services.CreateScope())
                {

                    var ns = scope.ServiceProvider.GetRequiredService<INetsuiteService>();

                    var parameters = new Dictionary<string, string>
                        {
                            { "method", "getContact" },
                            { "contactId", "5" }
                        };

                    var x = await ns.GetAsync<string>(parameters);

                    Console.WriteLine("Received response from netsuite: " + x);
                }

                host.Run();

                Console.WriteLine("Host exited cleanly");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal exception in host: " + ex.ToString());
                return 1;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
