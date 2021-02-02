using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace OAUthSysToSys
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)

                .ConfigureAppConfiguration((context, config) =>
                {
                    IConfiguration configBuilt = config.Build();

                    var keyVaultEndpoint = new Uri(configBuilt["VaultUri"]);
                    config.AddAzureKeyVault(
                        keyVaultEndpoint,
                        new DefaultAzureCredential());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
