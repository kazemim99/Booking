using Booksy.Core.Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Design-time factory for creating ServiceCatalogDbContext instances for EF Core migrations
    /// </summary>
    public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
    {
        public ServiceCatalogDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ServiceCatalogDbContext>();

            // Use SQL Server with a placeholder connection string for migrations
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=BooksyServiceCatalog;Trusted_Connection=True;MultipleActiveResultSets=true",
                b => b.MigrationsAssembly("Booksy.ServiceCatalog.Infrastructure"));

            // Create mock services for design-time
            var currentUserService = new DesignTimeCurrentUserService();
            var dateTimeProvider = new DesignTimeDateTimeProvider();

            return new ServiceCatalogDbContext(optionsBuilder.Options, currentUserService, dateTimeProvider);
        }

        // Mock implementation for design-time
        private class DesignTimeCurrentUserService : ICurrentUserService
        {
            public string? UserId => "design-time-user";
            public string? Email => "design@time.com";
            public string? Name => "Design Time User";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => new List<string> { "Admin" };
            public IEnumerable<Claim> Claims => new List<Claim>();
            public string? IpAddress => "127.0.0.1";
            public string? UserAgent => "EF-Migrations";

            public bool IsInRole(string role) => role == "Admin";
            public string? GetClaimValue(string claimType) => null;
        }

        // Mock implementation for design-time
        private class DesignTimeDateTimeProvider : IDateTimeProvider
        {
            public DateTime UtcNow => DateTime.UtcNow;
            public DateTime Now => DateTime.Now;
            public DateTime UtcToday => DateTime.UtcNow.Date;
            public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
            public long UnixTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            public long UnixTimestampMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            public long ToUnixTimestamp(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            public DateTime FromUnixTimestamp(long timestamp) => DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
    }
}
