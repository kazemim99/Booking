using Booksy.API.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Booksy.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
           

            // Configure Serilog with centralized configuration

            try
            {
                Log.Information("========================================");
                Log.Information("Starting ServiceCatalog API");
                Log.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");
                Log.Information("========================================");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ServiceCatalog API terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.Information("Shutting down ServiceCatalog API");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilogWithConfiguration("ServiceCatalog.API")
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel();
                    webBuilder.UseUrls("http://localhost:5010");
                });
    }
}
