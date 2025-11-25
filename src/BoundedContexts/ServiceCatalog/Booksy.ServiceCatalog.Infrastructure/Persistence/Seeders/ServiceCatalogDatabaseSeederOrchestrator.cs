using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
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

                // 3. Staff (depends on Providers)
                new StaffSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<StaffSeeder>.Instance),

                // 4. BusinessHours (depends on Providers)
                new BusinessHoursSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BusinessHoursSeeder>.Instance),

                // 5. Services (depends on Providers)
                new ServiceSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceSeeder>.Instance),

                // 6. ServiceOptions (depends on Services)
                new ServiceOptionSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceOptionSeeder>.Instance),

                // 7. Availability (depends on Providers, Staff, BusinessHours, Services)
                // MUST run BEFORE BookingSeeder because bookings need availability slots
                new AvailabilitySeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AvailabilitySeeder>.Instance),

                // 8. Notification Templates (independent)
                new NotificationTemplateSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance),

                // 9. Bookings (depends on Providers, Staff, Services, ProviderAvailability)
                new BookingSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingSeeder>.Instance),

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
