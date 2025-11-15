using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Emit;
using BusinessHours = Booksy.ServiceCatalog.Domain.Entities.BusinessHours;
using HolidaySchedule = Booksy.ServiceCatalog.Domain.Entities.HolidaySchedule;
using ExceptionSchedule = Booksy.ServiceCatalog.Domain.Entities.ExceptionSchedule;
using Booksy.Core.Domain.Domain.Entities;

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
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Payout> Payouts => Set<Payout>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();
        public DbSet<ProviderAvailability> ProviderAvailability => Set<ProviderAvailability>();
        public DbSet<Review> Reviews => Set<Review>();

        // Entities
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
        public DbSet<HolidaySchedule> Holidays => Set<HolidaySchedule>();
        public DbSet<ExceptionSchedule> Exceptions => Set<ExceptionSchedule>();
        // ServiceOption and PriceTier are owned entities (OwnsMany) - not exposed as DbSets
        public DbSet<ProvinceCities> ProvinceCities => Set<ProvinceCities>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suppress PendingModelChangesWarning when there are no actual schema changes
            // This can happen when method signatures change but database schema doesn't
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            base.OnConfiguring(optionsBuilder);
        }

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
