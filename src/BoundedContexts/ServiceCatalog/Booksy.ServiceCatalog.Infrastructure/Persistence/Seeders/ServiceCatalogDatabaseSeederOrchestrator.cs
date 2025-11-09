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

                // 4. Services (depends on Providers)
                new ServiceSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceSeeder>.Instance),

                // 5. Notification Templates (independent)
                new NotificationTemplateSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance),

                // 6. Bookings (depends on Providers, Staff, Services)
                new BookingSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingSeeder>.Instance),

                // 7. Payments (depends on Bookings)
                new PaymentSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentSeeder>.Instance),

                // 8. Payouts (depends on Payments)
                new PayoutSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<PayoutSeeder>.Instance)
            };
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("=========================================");
                _logger.LogInformation("Starting ServiceCatalog Database Seeding");
                _logger.LogInformation("=========================================");

                var startTime = DateTime.UtcNow;
                var seederCount = 0;

                foreach (var seeder in _seeders)
                {
                    var seederName = seeder.GetType().Name;
                    _logger.LogInformation("Executing seeder: {SeederName}", seederName);

                    try
                    {
                        await seeder.SeedAsync(cancellationToken);
                        seederCount++;
                        _logger.LogInformation("✓ {SeederName} completed successfully", seederName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "✗ Error in {SeederName}", seederName);
                        throw;
                    }
                }

                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation("=========================================");
                _logger.LogInformation("ServiceCatalog Seeding Completed Successfully");
                _logger.LogInformation("Executed {Count} seeders in {Duration:c}", seederCount, duration);
                _logger.LogInformation("=========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ServiceCatalog Database Seeding Failed");
                throw;
            }
        }
    }
}
