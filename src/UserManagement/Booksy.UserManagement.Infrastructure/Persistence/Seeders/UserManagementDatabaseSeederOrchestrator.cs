using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Main orchestrator for seeding the UserManagement database
    /// Coordinates execution of all user-related seeders
    /// </summary>
    public sealed class UserManagementDatabaseSeederOrchestrator : ISeeder
    {
        private readonly UserManagementDbContext _context;
        private readonly ILogger<UserManagementDatabaseSeederOrchestrator> _logger;
        private readonly IEnumerable<ISeeder> _seeders;

        public UserManagementDatabaseSeederOrchestrator(
            UserManagementDbContext context,
            ILogger<UserManagementDatabaseSeederOrchestrator> logger)
        {
            _context = context;
            _logger = logger;

            // Initialize all seeders in correct dependency order
            _seeders = new List<ISeeder>
            {
                // Seed Iranian customers
                new CustomerSeeder(_context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerSeeder>.Instance)
            };
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("=========================================");
                _logger.LogInformation("Starting UserManagement Database Seeding");
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
                _logger.LogInformation("UserManagement Seeding Completed Successfully");
                _logger.LogInformation("Executed {Count} seeders in {Duration:c}", seederCount, duration);
                _logger.LogInformation("=========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserManagement Database Seeding Failed");
                throw;
            }
        }
    }
}
