using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds business hours for providers with Iranian business culture
    /// (Friday and some Saturday as weekend days)
    /// </summary>
    public sealed class BusinessHoursSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<BusinessHoursSeeder> _logger;
        private readonly Random _random = new Random(55667);

        public BusinessHoursSeeder(
            ServiceCatalogDbContext context,
            ILogger<BusinessHoursSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.BusinessHours.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("BusinessHours already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian business hours seeding...");

                var providers = await _context.Providers
                    .Include(p => p.BusinessHours)
                    .Where(p => !p.BusinessHours.Any())
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogWarning("No providers without business hours found.");
                    return;
                }

                foreach (var provider in providers)
                {
                    SetIranianBusinessHours(provider);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded business hours for {Count} providers", providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian business hours");
                throw;
            }
        }

        private void SetIranianBusinessHours(Domain.Aggregates.Provider provider)
        {
            // Iranian business culture:
            // - Weekend is Friday (and often Thursday afternoon or Saturday)
            // - Most businesses: Saturday-Wednesday 9AM-9PM or 10AM-10PM
            // - Thursday: Often half day (9AM-2PM) or normal hours
            // - Friday: Closed (Islamic weekend)
            // - Lunch break: 1PM-4PM (some businesses)

            var businessHours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

            // Determine business pattern based on provider type
            var pattern = DetermineBusinessPattern(provider.ProviderType);

            switch (pattern)
            {
                case BusinessPattern.StandardSalon:
                    // Saturday - Wednesday: 10:00 - 20:00
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    // Thursday: Half day
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(10, 0), new TimeOnly(14, 0));
                    // Friday: Closed (Islamic weekend)
                    businessHours[DayOfWeek.Friday] = (null, null);
                    break;

                case BusinessPattern.Spa:
                    // Saturday - Thursday: 9:00 - 21:00 (longer hours)
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    // Friday: Closed
                    businessHours[DayOfWeek.Friday] = (null, null);
                    break;

                case BusinessPattern.Clinic:
                    // Saturday - Thursday: 8:00 - 18:00 (medical hours)
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(8, 0), new TimeOnly(13, 0));
                    // Friday: Closed
                    businessHours[DayOfWeek.Friday] = (null, null);
                    break;

                case BusinessPattern.GymFitness:
                    // Saturday - Thursday: 6:00 - 22:00 (early and late hours)
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(6, 0), new TimeOnly(22, 0));
                    // Friday: Short hours (some gyms open on Friday)
                    businessHours[DayOfWeek.Friday] = (new TimeOnly(8, 0), new TimeOnly(14, 0));
                    break;

                case BusinessPattern.FlexibleBarbershop:
                    // Saturday - Wednesday: 9:00 - 21:00
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    // Thursday: Normal hours
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(9, 0), new TimeOnly(21, 0));
                    // Friday: Closed
                    businessHours[DayOfWeek.Friday] = (null, null);
                    break;
            }

            provider.SetBusinessHours(businessHours);
        }

        private BusinessPattern DetermineBusinessPattern(Domain.Enums.ProviderType providerType)
        {
            return providerType switch
            {
                Domain.Enums.ProviderType.Salon => BusinessPattern.StandardSalon,
                Domain.Enums.ProviderType.Spa => BusinessPattern.Spa,
                Domain.Enums.ProviderType.Clinic => BusinessPattern.Clinic,
                Domain.Enums.ProviderType.Medical => BusinessPattern.Clinic,
                Domain.Enums.ProviderType.GymFitness => BusinessPattern.GymFitness,
                Domain.Enums.ProviderType.Individual => BusinessPattern.FlexibleBarbershop,
                Domain.Enums.ProviderType.Professional => BusinessPattern.Clinic,
                _ => BusinessPattern.StandardSalon
            };
        }

        private enum BusinessPattern
        {
            StandardSalon,
            Spa,
            Clinic,
            GymFitness,
            FlexibleBarbershop
        }
    }
}
