//using Booksy.Core.Application.Abstractions.Services;
//using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.Extensions.Configuration;

//namespace Booksy.ServiceCatalog.Infrastructure.Persistence
//{
//    /// <summary>
//    /// Factory for creating ServiceCatalogDbContext at design time (for migrations)
//    /// </summary>
//    public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
//    {
//        public ServiceCatalogDbContext CreateDbContext(string[] args)
//        {
//            // Build configuration
//            var configuration = new ConfigurationBuilder()
//                .SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("appsettings.json", optional: true)
//                .AddJsonFile("appsettings.Development.json", optional: true)
//                .AddEnvironmentVariables()
//                .Build();

//            // Get connection string
//            var connectionString = configuration.GetConnectionString("DefaultConnection")
//                ?? "Server=localhost;Database=BooksyDev;Trusted_Connection=True;TrustServerCertificate=True;";

//            // Create DbContextOptions
//            var optionsBuilder = new DbContextOptionsBuilder<ServiceCatalogDbContext>();
//            optionsBuilder.UseSqlServer(connectionString,
//                b => b.MigrationsAssembly("Booksy.ServiceCatalog.Infrastructure"));

//            // Create mock services for design time
//            var mockCurrentUserService = new DesignTimeCurrentUserService();
//            var mockDateTimeProvider = new DesignTimeDateTimeProvider();

//            return new ServiceCatalogDbContext(optionsBuilder.Options, mockCurrentUserService, mockDateTimeProvider);
//        }

//        // Mock implementation for ICurrentUserService at design time
//        private class DesignTimeCurrentUserService : ICurrentUserService
//        {
//            public string? UserId => "design-time-user";
//            public string? Email => "design-time@booksy.com";
//            public bool IsAuthenticated => false;
//            public IEnumerable<string> Roles => Array.Empty<string>();
//        }

//        // Mock implementation for IDateTimeProvider at design time
//        private class DesignTimeDateTimeProvider : IDateTimeProvider
//        {
//            public DateTime UtcNow => DateTime.UtcNow;
//            public DateTime Now => DateTime.Now;
//            public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
//            public DateTimeOffset NowOffset => DateTimeOffset.Now;
//        }
//    }
//}
