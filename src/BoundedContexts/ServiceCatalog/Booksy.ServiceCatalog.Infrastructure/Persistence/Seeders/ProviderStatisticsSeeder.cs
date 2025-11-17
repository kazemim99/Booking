using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Calculates and updates provider statistics (ratings, booking counts) based on existing booking data
    /// Uses realistic distribution: 50% excellent (4.5-5.0), 25% good (3.5-4.4), 15% average (2.5-3.4), 10% poor
    /// </summary>
    public sealed class ProviderStatisticsSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ProviderStatisticsSeeder> _logger;
        private readonly Random _random = new Random(12345); // Deterministic for consistent results

        public ProviderStatisticsSeeder(
            ServiceCatalogDbContext context,
            ILogger<ProviderStatisticsSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting provider statistics calculation...");

                var providers = await _context.Providers
                    .Include(p => p.Services)
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogWarning("No providers found to calculate statistics.");
                    return;
                }

                int updatedCount = 0;

                foreach (var provider in providers)
                {
                    // Calculate realistic rating based on provider type and quality tier
                    var rating = GenerateRealisticRating(provider.ProviderType);

                    // Calculate review count based on booking count (60% of completed bookings get reviews)
                    var completedBookingsCount = await _context.Bookings
                        .Where(b => b.ProviderId == provider.Id.Value
                                 && b.Status == Domain.Enums.BookingStatus.Completed)
                        .CountAsync(cancellationToken);

                    var reviewCount = (int)(completedBookingsCount * 0.6); // 60% review rate

                    // TODO: AverageRating is now calculated property, update via domain method if needed
                    // provider.UpdateRating(rating.Value); // Or similar domain method

                    updatedCount++;

                    _logger.LogDebug(
                        "Provider {ProviderId} ({BusinessName}): Rating {Rating} ({ReviewCount} reviews), {BookingCount} bookings",
                        provider.Id.Value,
                        provider.Profile.BusinessName,
                        rating.Value,
                        reviewCount,
                        completedBookingsCount);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully calculated statistics for {Count} providers",
                    updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating provider statistics");
                throw;
            }
        }

        /// <summary>
        /// Generate realistic rating distribution
        /// Distribution: 50% excellent (4.5-5.0), 25% good (3.5-4.4), 15% average (2.5-3.4), 10% poor (1.5-2.4)
        /// </summary>
        private Rating GenerateRealisticRating(Domain.Enums.ProviderType providerType)
        {
            var distribution = _random.Next(100);

            decimal ratingValue;
            int reviewCount;

            if (distribution < 50) // 50% excellent
            {
                ratingValue = GenerateRatingInRange(4.5m, 5.0m);
                reviewCount = _random.Next(20, 150); // Popular providers have more reviews
            }
            else if (distribution < 75) // 25% good
            {
                ratingValue = GenerateRatingInRange(3.5m, 4.4m);
                reviewCount = _random.Next(10, 80);
            }
            else if (distribution < 90) // 15% average
            {
                ratingValue = GenerateRatingInRange(2.5m, 3.4m);
                reviewCount = _random.Next(5, 40);
            }
            else // 10% poor
            {
                ratingValue = GenerateRatingInRange(1.5m, 2.4m);
                reviewCount = _random.Next(3, 20);
            }

            // Premium provider types (Spa, Clinic) tend to have slightly higher ratings
            if (providerType == Domain.Enums.ProviderType.Spa ||
                providerType == Domain.Enums.ProviderType.Clinic)
            {
                ratingValue = Math.Min(5.0m, ratingValue + 0.3m);
            }

            // Round to nearest 0.5 (half-star increments)
            ratingValue = Math.Round(ratingValue * 2, MidpointRounding.AwayFromZero) / 2;
            ratingValue = Math.Max(1.0m, Math.Min(5.0m, ratingValue));

            return Rating.Create(ratingValue, reviewCount);
        }

        /// <summary>
        /// Generate rating value within a specific range
        /// </summary>
        private decimal GenerateRatingInRange(decimal min, decimal max)
        {
            var range = max - min;
            var randomValue = (decimal)_random.NextDouble() * range;
            return min + randomValue;
        }
    }
}
