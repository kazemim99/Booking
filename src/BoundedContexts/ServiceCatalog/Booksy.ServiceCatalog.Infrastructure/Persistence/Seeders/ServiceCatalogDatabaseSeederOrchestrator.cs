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

                // 7. Notification Templates (independent)
                new NotificationTemplateSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance),

                // 8. Bookings (depends on Providers, Staff, Services)
                new BookingSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingSeeder>.Instance),

                // 9. Payments (depends on Bookings)
                new PaymentSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentSeeder>.Instance),

                // 10. Payouts (depends on Payments)
                new PayoutSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<PayoutSeeder>.Instance),

                // 11. UserNotificationPreferences (depends on Bookings for customer IDs)
                new UserNotificationPreferencesSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<UserNotificationPreferencesSeeder>.Instance)
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
