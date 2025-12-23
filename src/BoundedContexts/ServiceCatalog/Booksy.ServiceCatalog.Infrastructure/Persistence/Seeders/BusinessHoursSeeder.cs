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
                // ✅ DDD: Query through Provider aggregate, not child entity directly
                var providers = await _context.Providers
                    .Include(p => p.BusinessHours)
                    .Where(p => !p.BusinessHours.Any())
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogInformation("BusinessHours already seeded for all providers. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian business hours seeding...");

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
            // - Weekend is Friday (Islamic weekend)
            // - Most businesses: Saturday-Thursday 9AM-9PM or 10AM-10PM
            // - Thursday: FULL DAY (modern business practices)
            // - Friday: Closed (Islamic weekend)
            // - Lunch break: 1PM-4PM (some businesses)

            var businessHours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

            // Determine business pattern based on provider type
            var pattern = DetermineBusinessPattern(provider.PrimaryCategory);

            switch (pattern)
            {
                case BusinessPattern.StandardSalon:
                    // Saturday - Thursday: 10:00 - 20:00 (Thursday now FULL day)
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
                    // Thursday: FULL day (changed from half day)
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(10, 0), new TimeOnly(20, 0));
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
                    // Saturday - Thursday: 8:00 - 18:00 (medical hours, Thursday now FULL day)
                    businessHours[DayOfWeek.Saturday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Sunday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Monday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Tuesday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    businessHours[DayOfWeek.Wednesday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
                    // Thursday: FULL day (changed from half day)
                    businessHours[DayOfWeek.Thursday] = (new TimeOnly(8, 0), new TimeOnly(18, 0));
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

        private BusinessPattern DetermineBusinessPattern(Domain.Enums.ServiceCategory category)
        {
            return category switch
            {
                Domain.Enums.ServiceCategory.HairSalon => BusinessPattern.StandardSalon,
                Domain.Enums.ServiceCategory.Barbershop => BusinessPattern.FlexibleBarbershop,
                Domain.Enums.ServiceCategory.BeautySalon => BusinessPattern.StandardSalon,
                Domain.Enums.ServiceCategory.NailSalon => BusinessPattern.StandardSalon,
                Domain.Enums.ServiceCategory.Spa => BusinessPattern.Spa,
                Domain.Enums.ServiceCategory.Massage => BusinessPattern.Spa,
                Domain.Enums.ServiceCategory.Gym => BusinessPattern.GymFitness,
                Domain.Enums.ServiceCategory.Yoga => BusinessPattern.GymFitness,
                Domain.Enums.ServiceCategory.MedicalClinic => BusinessPattern.Clinic,
                Domain.Enums.ServiceCategory.Dental => BusinessPattern.Clinic,
                Domain.Enums.ServiceCategory.Physiotherapy => BusinessPattern.Clinic,
                Domain.Enums.ServiceCategory.Tutoring => BusinessPattern.Clinic,
                Domain.Enums.ServiceCategory.Automotive => BusinessPattern.Clinic,
                Domain.Enums.ServiceCategory.HomeServices => BusinessPattern.FlexibleBarbershop,
                Domain.Enums.ServiceCategory.PetCare => BusinessPattern.StandardSalon,
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
