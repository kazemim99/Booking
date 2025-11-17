// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Context/UserManagementDbContextFactory.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Core.Application.Abstractions.Services;
using System.Security.Claims;
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.UserManagement.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Design-time factory for EF Core migrations
    /// </summary>
    public class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
    {
        public UserManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();

            // Use a connection string for migrations (will be replaced at runtime)
            optionsBuilder.UseNpgsql("Host=localhost;Database=booksy;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("Booksy.UserManagement.Infrastructure"));

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
