// ========================================
// AsanRezerve.UserManagement.Infrastructure/Persistence/Context/UserManagementDbContext.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.UserManagement.Domain.Entities;
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Aggregates.CustomerAggregate;
using AsanRezerve.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using AsanRezerve.UserManagement.Domain.ReadModels;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.Persistence.Converters;
using System.Threading;

namespace AsanRezerve.UserManagement.Infrastructure.Persistence.Context
{
    public class UserManagementDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;
        private readonly IDateTimeProvider? _dateTimeProvider;
        private IDbContextTransaction? _currentTransaction;
        //private readonly List<IDomainEvent> _domainEvents = new();
        private readonly IDomainEventDispatcher _eventDispatcher;


        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<PhoneVerification> PhoneVerifications { get; set; } = null!;
        public DbSet<CustomerBookingHistoryEntry> CustomerBookingHistory { get; set; } = null!;

        public bool HasActiveTransaction => throw new NotImplementedException();

   
        // Constructor for runtime with DI
        public UserManagementDbContext(
            DbContextOptions<UserManagementDbContext> options,
            ICurrentUserService currentUserService,
            IDateTimeProvider dateTimeProvider,
            IDomainEventDispatcher eventDispatcher)
            : base(options)
        {
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
            _eventDispatcher = eventDispatcher;
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Every DateTime is a UTC instant; see UtcDateTimeConverter for why this has to be enforced here.
            configurationBuilder.UseUtcDateTimes();
            base.ConfigureConventions(configurationBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Set default schema
            modelBuilder.HasDefaultSchema("user_management");

            // Apply all configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserManagementDbContext).Assembly);

            // Global query filters for soft delete
            modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != Domain.Enums.UserStatus.Deleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {



            // Update audit fields
            UpdateAuditableEntities();

            // Collect domain events before saving
            await CollectDomainEvents(cancellationToken);



            // Save changes to database
            var result = await base.SaveChangesAsync(cancellationToken);

            return result;
        }



        private void UpdateAuditableEntities()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));



         


            var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
            var userId = _currentUserService?.UserId ?? "System";

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = userId;
                }
                else
                {
                    entry.Property(nameof(IAuditableEntity.LastModifiedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.LastModifiedBy)).CurrentValue = userId;
                }
            }
        }

        private async Task CollectDomainEvents(CancellationToken cancellationToken)
        {
            var aggregates = ChangeTracker.Entries<IAggregateRoot>()
                .Where(x => x.Entity.DomainEvents?.Any() == true)
                .Select(x => x.Entity)
                .ToList();

            await _eventDispatcher.DispatchEventsAsync(aggregates, cancellationToken);

            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            base.Dispose();
        }

      
    }
}

