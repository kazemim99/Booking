using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds sample staff members to providers
    /// </summary>
    public sealed class StaffDataSeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<StaffDataSeeder> _logger;

        public StaffDataSeeder(
            ServiceCatalogDbContext context,
            ILogger<StaffDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get providers that don't have staff yet
                var providers = await _context.Providers
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogInformation("All providers already have staff. Skipping...");
                    return;
                }

                _logger.LogInformation("Adding staff to {Count} providers", providers.Count);

                var totalStaffAdded = 0;


                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added {TotalStaff} staff members to {ProviderCount} providers",
                    totalStaffAdded, providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding staff data");
                throw;
            }
        }

    }
}
