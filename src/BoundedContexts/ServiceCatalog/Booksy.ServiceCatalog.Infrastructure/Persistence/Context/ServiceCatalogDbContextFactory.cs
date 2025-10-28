//using Booksy.Core.Application.Abstractions.Services;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.Extensions.Configuration;

//namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Context
//{
//    /// <summary>
//    /// Design-time factory for ServiceCatalogDbContext to support EF Core migrations
//    /// </summary>
//    public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
//    {
//        public ServiceCatalogDbContext CreateDbContext(string[] args)
//        {
//            var optionsBuilder = new DbContextOptionsBuilder<ServiceCatalogDbContext>();
//            var connectionString = "Host=localhost;Port=54321;Database=BooksyServiceCatalog;Username=postgres;Password=postgres";

//            optionsBuilder.UseNpgsql(connectionString);

//            // Create mock services for design-time
//            var currentUserService = new DesignTimeCurrentUserService();
//            var dateTimeProvider = new DesignTimeDateTimeProvider();

//            return new ServiceCatalogDbContext(optionsBuilder.Options, currentUserService, dateTimeProvider);
//        }
//    }

//    /// <summary>
//    /// Mock current user service for design-time context creation
//    /// </summary>
//    internal class DesignTimeCurrentUserService : ICurrentUserService
//    {
//        public string UserId => "design-time-user";
//        public string? Email => "design-time@booksy.com";
//        public string? Name => "Design Time User";
//        public bool IsAuthenticated => false;
//        public IEnumerable<string> Roles => Enumerable.Empty<string>();
//        public IEnumerable<System.Security.Claims.Claim> Claims => Enumerable.Empty<System.Security.Claims.Claim>();
//        public string? IpAddress => "127.0.0.1";
//        public string? UserAgent => "DesignTime";

//        public bool IsInRole(string role) => false;
//        public string? GetClaimValue(string claimType) => null;
//    }

//    /// <summary>
//    /// Mock date time provider for design-time context creation
//    /// </summary>
//    internal class DesignTimeDateTimeProvider : IDateTimeProvider
//    {
//        public DateTime UtcNow => DateTime.UtcNow;
//        public DateTime Now => DateTime.Now;
//        public DateTime UtcToday => DateTime.UtcNow.Date;
//        public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
//        public long UnixTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//        public long UnixTimestampMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

//        public long ToUnixTimestamp(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();
//        public DateTime FromUnixTimestamp(long timestamp) => DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
//    }
//}
