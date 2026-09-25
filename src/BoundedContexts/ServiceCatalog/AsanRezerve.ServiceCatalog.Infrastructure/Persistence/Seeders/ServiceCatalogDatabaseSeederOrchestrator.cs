using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Main orchestrator for seeding the ServiceCatalog database
    /// Coordinates execution of all seeders in correct order
    /// </summary>
    public sealed class ServiceCatalogDatabaseSeederOrchestrator : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceCatalogDatabaseSeederOrchestrator> _logger;
        private readonly IEnumerable<ISeeder> _seeders;

        public ServiceCatalogDatabaseSeederOrchestrator(
            ServiceCatalogDbContext context,
            ILogger<ServiceCatalogDatabaseSeederOrchestrator> logger)
        {
            _context = context;
            _logger = logger;

            // Initialize all seeders in correct dependency order
            _seeders = new List<ISeeder>
            {
                // 1. Province/Cities first (independent)
                new ProvinceCitiesSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ProvinceCitiesSeeder>.Instance),

                // 2. Providers (independent)
                new ProviderSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ProviderSeeder>.Instance),

                // 3. Staff (depends on Providers).
                // Seeded as unclaimed OrganizationMemberships — the same mechanism a salon uses to add a colleague
                // who does not use the app yet. This MUST precede ServiceActivationSeeder: an organization's service
                // cannot be activated (and therefore cannot be booked) until somebody is qualified to perform it.
                new StaffSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<StaffSeeder>.Instance),

                // 4. BusinessHours (depends on Providers)
                new BusinessHoursSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BusinessHoursSeeder>.Instance),

                // 5. Services (depends on Providers) — created as Draft
                new ServiceSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceSeeder>.Instance),

                // 6. Service activation (depends on Staff + Services)
                // Qualifies each provider's members for its services and activates them, so the availability engine
                // will offer slots. Without this every seeded service stays Draft and the app shows no free times.
                new ServiceActivationSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceActivationSeeder>.Instance),

                // 7. ServiceOptions (depends on Services)
                new ServiceOptionSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceOptionSeeder>.Instance),

                // 8. Availability (depends on Providers, Staff, BusinessHours, Services)
                // MUST run BEFORE BookingSeeder because bookings need availability slots
                new AvailabilitySeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AvailabilitySeeder>.Instance),

                // 9. Notification Templates (independent)
                new NotificationTemplateSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance),


                // 10. Reviews (depends on Bookings - only completed bookings can have reviews)
                new ReviewSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ReviewSeeder>.Instance),

                // 11. Payments (depends on Bookings) - DISABLED due to EF Core owned entity tracking issues
                // TODO: Fix PaymentSeeder to handle multiple owned Money entities with same currency
                // new PaymentSeeder(_context,
                //     Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentSeeder>.Instance),

                // 12. Payouts (depends on Payments) - DISABLED because it depends on Payments
                // new PayoutSeeder(_context,
                //     Microsoft.Extensions.Logging.Abstractions.NullLogger<PayoutSeeder>.Instance),

                // 13. UserNotificationPreferences (depends on Bookings for customer IDs)
                new UserNotificationPreferencesSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<UserNotificationPreferencesSeeder>.Instance),

                // 14. Provider Statistics (depends on Bookings and Reviews for calculation)
                new ProviderStatisticsSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ProviderStatisticsSeeder>.Instance)
            };
        }

        /// <summary>
        /// Seeds ONLY the reference data the app needs in every environment, production included:
        /// Iran's province/city hierarchy (read by GET /api/v1/Locations/hierarchy and the provider
        /// app's onboarding city picker) and the global notification templates that
        /// INotificationTemplateService renders. Both seeders are idempotent — each returns early once
        /// any row exists — so this is safe to run on every startup.
        /// <para>
        /// Deliberately separate from <see cref="SeedAsync"/>, which additionally runs ProviderSeeder,
        /// StaffSeeder, ServiceSeeder, AvailabilitySeeder, ReviewSeeder and more: fake demo data that
        /// must never reach a production database. Before this split both kinds sat behind the single
        /// Database:SeedOnStartup gate (default: IsDevelopment()), so production got neither — the
        /// ProvinceCities table was empty and the onboarding city picker could never match anything.
        /// </para>
        /// </summary>
        public async Task SeedReferenceDataAsync(CancellationToken cancellationToken = default)
        {
            var referenceSeeders = new ISeeder[]
            {
                new ProvinceCitiesSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ProvinceCitiesSeeder>.Instance),
                new NotificationTemplateSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance),
            };

            foreach (var seeder in referenceSeeders)
            {
                try
                {
                    await seeder.SeedAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Logged at Error rather than rethrown: a missing reference table degrades one
                    // feature, whereas failing startup would take the whole API down on a shared box.
                    // It must still be loud — a silent empty table is exactly how this went unnoticed.
                    _logger.LogError(ex, "Reference data seeding failed: {Seeder}", seeder.GetType().Name);
                }
            }
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {

                var startTime = DateTime.UtcNow;
                var seederCount = 0;

                foreach (var seeder in _seeders)
                {
                    var seederName = seeder.GetType().Name;

                    try
                    {
                        await seeder.SeedAsync(cancellationToken);
                        seederCount++;
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError(ex, $"Entity: { seederName}");

                        //throw new ApplicationException($"Entity: {seederName}-Message: {ex.Message}");
                    }
                }
                var duration = DateTime.UtcNow - startTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ServiceCatalog Database Seeding Failed");
                throw;
            }
        }
    }
}
