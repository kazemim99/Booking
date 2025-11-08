using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        // Entities
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
        public DbSet<HolidaySchedule> Holidays => Set<HolidaySchedule>();
        public DbSet<ExceptionSchedule> Exceptions => Set<ExceptionSchedule>();
        public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
        public DbSet<PriceTier> PriceTiers => Set<PriceTier>();
        public DbSet<ProvinceCities> ProvinceCities => Set<ProvinceCities>();
        public DbSet<BookingHistorySnapshot> BookingHistorySnapshots => Set<BookingHistorySnapshot>();

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
            await PersistBookingMementosAsync(cancellationToken);
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

        private async Task PersistBookingMementosAsync(CancellationToken cancellationToken)
        {
            // Find all modified or added Booking aggregates
            var bookingEntries = ChangeTracker.Entries<Booking>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .ToList();

            foreach (var entry in bookingEntries)
            {
                var booking = entry.Entity;
                var mementos = booking.GetMementoHistory();

                // Get existing snapshots for this booking
                var existingSnapshots = await BookingHistorySnapshots
                    .Where(s => s.BookingId == booking.Id)
                    .Select(s => s.StateId)
                    .ToListAsync(cancellationToken);

                // Persist new mementos that don't already exist in the database
                foreach (var memento in mementos)
                {
                    if (!existingSnapshots.Contains(memento.StateId))
                    {
                        var stateJson = System.Text.Json.JsonSerializer.Serialize(
                            memento.State,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                WriteIndented = false,
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                            });

                        var snapshot = BookingHistorySnapshot.Create(
                            booking.Id,
                            memento.StateId,
                            memento.StateName,
                            stateJson,
                            memento.CreatedAt,
                            memento.TriggeredBy ?? _currentUserService.UserId,
                            memento.Description);

                        await BookingHistorySnapshots.AddAsync(snapshot, cancellationToken);
                    }
                }
            }
        }
    }
}
