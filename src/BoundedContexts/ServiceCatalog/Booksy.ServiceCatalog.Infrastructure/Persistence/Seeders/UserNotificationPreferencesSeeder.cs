using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds user notification preferences with default Iranian settings
    /// </summary>
    public sealed class UserNotificationPreferencesSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<UserNotificationPreferencesSeeder> _logger;

        public UserNotificationPreferencesSeeder(
            ServiceCatalogDbContext context,
            ILogger<UserNotificationPreferencesSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.UserNotificationPreferences.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("UserNotificationPreferences already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting user notification preferences seeding...");

                // Get all unique customer IDs from bookings
                var customerIds = await _context.Bookings
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (!customerIds.Any())
                {
                    _logger.LogWarning("No customers found for notification preferences seeding.");
                    return;
                }

                var preferences = new List<UserNotificationPreferences>();

                foreach (var customerId in customerIds)
                {
                    // Create default preferences for each customer
                    var userPreference = UserNotificationPreferences.CreateDefault(customerId);
                    preferences.Add(userPreference);
                }

                await _context.UserNotificationPreferences.AddRangeAsync(preferences, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded notification preferences for {Count} users", preferences.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding user notification preferences");
                throw;
            }
        }
    }
}
