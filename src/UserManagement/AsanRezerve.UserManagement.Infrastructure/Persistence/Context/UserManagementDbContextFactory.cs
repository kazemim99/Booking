// ========================================
// AsanRezerve.UserManagement.Infrastructure/Persistence/Context/UserManagementDbContextFactory.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Core.Application.Abstractions.Services;
using System.Security.Claims;
using AsanRezerve.Core.Domain.Abstractions.Events;

namespace AsanRezerve.UserManagement.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Design-time factory for EF Core migrations
    /// </summary>
    public class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
    {
        public UserManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();

            // Honour the same configuration sources the CLI operator expects, instead of a
            // hardcoded localhost/postgres/postgres string that silently ignored both
            // appsettings.json and ConnectionStrings__DefaultConnection.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=asanrezerve;Username=postgres;Password=postgres";

            // Go through the SHARED configuration rather than hand-rolling UseNpgsql here.
            // This type's own doc comment warns that a hand-rolled builder "silently differs"
            // from the real one; that is precisely what happened — omitting
            // MigrationsHistoryTable pointed the EF CLI at the DEFAULT
            // public.__EFMigrationsHistory instead of user_management's, so the CLI saw an
            // empty history on a fully migrated database and tried to re-run every migration.
            UserManagementDbContextOptions.Configure(optionsBuilder, connectionString);

            // Create mock services for design-time
            var mockCurrentUserService = new MockCurrentUserService();
            var mockDateTimeProvider = new MockDateTimeProvider();
            var mockEventDispatcher = new MockEventDispatcher();

            return new UserManagementDbContext(
                optionsBuilder.Options,
                mockCurrentUserService,
                mockDateTimeProvider,
                mockEventDispatcher);
        }

        // Mock implementations for design-time
        private class MockCurrentUserService : ICurrentUserService
        {
            public string? UserId => "System";
            public string? UserName => "System";
            public bool IsAuthenticated => false;

            public string? Email => throw new NotImplementedException();

            public string? Name => throw new NotImplementedException();

            public IEnumerable<string> Roles => throw new NotImplementedException();

            public IEnumerable<Claim> Claims => throw new NotImplementedException();

            public string? IpAddress => throw new NotImplementedException();

            public string? UserAgent => throw new NotImplementedException();

            public string? GetClaimValue(string claimType)
            {
                throw new NotImplementedException();
            }

            public bool IsInRole(string role)
            {
                throw new NotImplementedException();
            }
        }

        private class MockDateTimeProvider : IDateTimeProvider
        {
            public DateTime UtcNow => DateTime.UtcNow;
            public DateTime Now => DateTime.Now;

            public DateTime UtcToday => throw new NotImplementedException();

            public DateOnly Today => throw new NotImplementedException();

            public long UnixTimestamp => throw new NotImplementedException();

            public long UnixTimestampMilliseconds => throw new NotImplementedException();

            public DateTime FromUnixTimestamp(long timestamp)
            {
                throw new NotImplementedException();
            }

            public long ToUnixTimestamp(DateTime dateTime)
            {
                throw new NotImplementedException();
            }
        }

        private class MockEventDispatcher : IDomainEventDispatcher
        {
            public Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task DispatchEventsAsync(IEnumerable<Core.Domain.Abstractions.Entities.IAggregateRoot> aggregates, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
