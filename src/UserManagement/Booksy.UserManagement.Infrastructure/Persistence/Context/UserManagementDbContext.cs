// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Context/UserManagementDbContext.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using System.Threading;

namespace Booksy.UserManagement.Infrastructure.Persistence.Context
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

