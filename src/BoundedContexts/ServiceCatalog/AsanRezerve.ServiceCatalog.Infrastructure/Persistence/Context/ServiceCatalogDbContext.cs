using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AsanRezerve.Core.Domain.Domain.Entities;
using AsanRezerve.Infrastructure.Core.Persistence.Converters;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context
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
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<Idempotency.IdempotencyReservation> IdempotencyReservations => Set<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Idempotency.IdempotencyReservation>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.NotificationDelivery> NotificationDeliveries
            => Set<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.NotificationDelivery>();
        public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public DbSet<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.NotificationOutboxEntry> NotificationOutbox
            => Set<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.NotificationOutboxEntry>();
        public DbSet<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.DeviceToken> DeviceTokens
            => Set<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications.DeviceToken>();
        public DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();
        public DbSet<ProviderAvailability> ProviderAvailability => Set<ProviderAvailability>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();
        public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
        public DbSet<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Reviews.ProviderRatingSummary> ProviderRatingSummaries
            => Set<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Reviews.ProviderRatingSummary>();
        public DbSet<ProviderInvitation> ProviderInvitations => Set<ProviderInvitation>();
        public DbSet<ProviderCustomer> ProviderCustomers => Set<ProviderCustomer>();
        public DbSet<Domain.Aggregates.PromotionAggregate.Promotion> Promotions => Set<Domain.Aggregates.PromotionAggregate.Promotion>();
        public DbSet<Domain.Aggregates.PromotionAggregate.CampaignEnrollment> CampaignEnrollments => Set<Domain.Aggregates.PromotionAggregate.CampaignEnrollment>();
        public DbSet<Domain.Aggregates.PromotionAggregate.PromotionRedemption> PromotionRedemptions => Set<Domain.Aggregates.PromotionAggregate.PromotionRedemption>();
        public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
        public DbSet<MembershipAuditEntry> MembershipAuditEntries => Set<MembershipAuditEntry>();

        // Reference Data (not part of aggregates)
        public DbSet<ProvinceCities> ProvinceCities => Set<ProvinceCities>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suppress PendingModelChangesWarning when there are no actual schema changes
            // This can happen when method signatures change but database schema doesn't
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            base.OnConfiguring(optionsBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Every DateTime is a UTC instant; see UtcDateTimeConverter for why this has to be enforced here.
            configurationBuilder.UseUtcDateTimes();
            base.ConfigureConventions(configurationBuilder);
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
            EnforceLedgerAppendOnly();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// The ledger is an immutable, append-only audit log: once a <see cref="LedgerEntry"/> is committed it must
        /// never be updated or deleted. Corrections are made by posting a compensating <c>LedgerTransaction</c>. This
        /// guard makes that a hard invariant — any attempt to modify or delete a ledger entry throws before it can be
        /// written, rather than relying on convention.
        /// </summary>
        private void EnforceLedgerAppendOnly()
        {
            foreach (var entry in ChangeTracker.Entries<LedgerEntry>())
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException(
                        $"Ledger entries are immutable (append-only): a {entry.State} operation on LedgerEntry " +
                        $"'{entry.Entity.Id}' is not allowed. Post a compensating transaction instead.");
                }
            }
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
