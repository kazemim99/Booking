using AsanRezerve.Core.Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Design-time factory for creating ServiceCatalogDbContext instances for EF Core migrations
    /// </summary>
    public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
    {
        public ServiceCatalogDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ServiceCatalogDbContext>();

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=AsanRezerveDev;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseNpgsql(
                connectionString,
                b =>
                {
                    b.MigrationsAssembly("AsanRezerve.ServiceCatalog.Infrastructure");

                    // MUST match the runtime registration in
                    // ServiceCatalogInfrastructureExtensions, which puts the history table in
                    // the ServiceCatalog schema. Omitting it here made the EF CLI read the
                    // DEFAULT public.__EFMigrationsHistory — an empty table — so
                    // `dotnet ef database update` concluded that nothing had ever been applied
                    // and tried to re-run Init against a fully populated database:
                    // "42P07: relation \"Bookings\" already exists". The app migrates itself
                    // correctly at startup, so this only ever bit the CLI, which is exactly
                    // what the deployment runbook tells an operator to use. It also left a
                    // stray empty public.__EFMigrationsHistory behind on any database the CLI
                    // was pointed at.
                    b.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog");
                });

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
