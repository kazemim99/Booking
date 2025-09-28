using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Context
{
    public sealed class ServiceCatalogDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;



        public ServiceCatalogDbContext(
            DbContextOptions<ServiceCatalogDbContext> options, ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
            : base(options)
        {
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
        }

        // Aggregate Roots
        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<Service> Services => Set<Service>();

        // Entities
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
        public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
        public DbSet<PriceTier> PriceTiers => Set<PriceTier>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                // Check if it's an aggregate root
                if (typeof(IAggregateRoot).IsAssignableFrom(clrType))
                {
                    // Ignore the DomainEvents property
                    var property = clrType.GetProperty("DomainEvents");
                    if (property != null)
                    {
                        entityType.RemoveProperty(property.Name);
                    }
                }
            }



            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProviderConfiguration).Assembly);


            modelBuilder.HasDefaultSchema("ServiceCatalog");

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditableEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditableEntities()
        {
            var currentUserId = _currentUserService.UserId;
            var currentTime = _dateTimeProvider.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.SetCreatedAt(currentTime);
                        entry.Entity.SetCreatedBy(currentUserId);
                        break;

                    case EntityState.Modified:
                        entry.Entity.SetLastModifiedAt(currentTime);
                        entry.Entity.SetLastModifiedBy(currentUserId);
                        break;
                }
            }
        }
    }
}
