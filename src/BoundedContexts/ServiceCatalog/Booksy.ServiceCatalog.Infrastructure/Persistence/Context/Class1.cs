//using Booksy.Core.Application.Abstractions.Services;
//using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;

//public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
//{
//    public ServiceCatalogDbContext CreateDbContext(string[] args)
//    {
//        // Build configuration from appsettings files
//        var configuration = BuildConfiguration(args);

//        // Create service collection for DI
//        var services = new ServiceCollection();


//        // Build service provider
//        var serviceProvider = services.BuildServiceProvider();

//        // Get DbContextOptions from DI container
//        var options = serviceProvider.GetRequiredService<DbContextOptions<ServiceCatalogDbContext>>();
//        var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
//        var dateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();

//        return new ServiceCatalogDbContext(options, currentUserService, dateTimeProvider);
//    }

//    private static IConfiguration BuildConfiguration(string[] args)
//    {
//        // Determine environment
//        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

//        // Parse command line arguments for environment override
//        var environmentArg = args?.FirstOrDefault(a => a.StartsWith("--environment="));
//        if (!string.IsNullOrEmpty(environmentArg))
//        {
//            environment = environmentArg.Split('=')[1];
//        }

//        Console.WriteLine($"[DbContextFactory] Using environment: {environment}");

//        var builder = new ConfigurationBuilder()
//            .SetBasePath(GetConfigurationBasePath())
//            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
//            .AddEnvironmentVariables();

//        // Add command line arguments if provided
//        if (args?.Length > 0)
//        {
//            builder.AddCommandLine(args);
//        }

//        var configuration = builder.Build();

//        // Log connection string (masked for security)
//        var connectionString = configuration.GetConnectionString("DefaultConnection");
//        if (!string.IsNullOrEmpty(connectionString))
//        {
//            var maskedConnectionString = MaskConnectionString(connectionString);
//            Console.WriteLine($"[DbContextFactory] Connection string: {maskedConnectionString}");
//        }
//        else
//        {
//            Console.WriteLine("[DbContextFactory] WARNING: No connection string found!");
//        }

//        return configuration;
//    }


//    private static string GetConfigurationBasePath()
//    {
//        // Try to find the API project directory with appsettings.json
//        var currentDirectory = Directory.GetCurrentDirectory();
//        var searchPaths = new[]
//        {
//            currentDirectory, // Current directory (Infrastructure project)
//            Path.Combine(currentDirectory, "..", "Booksy.ServiceCatalog.API"), // Sibling API project
//            Path.Combine(currentDirectory, "..", "..", "src", "BoundedContexts", "ServiceCatalog", "Booksy.ServiceCatalog.API"), // From solution root
//            Path.GetDirectoryName(typeof(ServiceCatalogDbContextFactory).Assembly.Location) ?? currentDirectory // Assembly location
//        };

//        foreach (var searchPath in searchPaths)
//        {
//            if (Directory.Exists(searchPath))
//            {
//                var appsettingsPath = Path.Combine(searchPath, "appsettings.json");
//                if (File.Exists(appsettingsPath))
//                {
//                    Console.WriteLine($"[DbContextFactory] Found appsettings.json at: {searchPath}");
//                    return searchPath;
//                }
//            }
//        }

//        // Fallback to current directory
//        Console.WriteLine($"[DbContextFactory] Using fallback path: {currentDirectory}");
//        return currentDirectory;
//    }

//    private static string MaskConnectionString(string connectionString)
//    {
//        // Mask password in connection string for logging
//        return System.Text.RegularExpressions.Regex.Replace(
//            connectionString,
//            @"Password=([^;]+)",
//            "Password=***",
//            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
//    }
//}
