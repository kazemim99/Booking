using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds staff members with Iranian/Persian names to providers
    /// </summary>
    public sealed class StaffSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<StaffSeeder> _logger;

        // Persian first names (male)
        private readonly string[] _persianMaleFirstNames = new[]
        {
            "علی", "رضا", "محمد", "حسین", "امیر", "مهدی", "سعید", "مسعود",
            "فرهاد", "کامران", "بهروز", "داریوش", "کیوان", "پیمان", "آرش", "سهراب"
        };

        // Persian first names (female)
        private readonly string[] _persianFemaleFirstNames = new[]
        {
            "فاطمه", "زهرا", "مریم", "سارا", "نگار", "لیلا", "نیلوفر", "شیرین",
            "مهناز", "پریسا", "نازنین", "پریناز", "شیدا", "آناهیتا", "طاها", "سمانه"
        };

        // Persian last names
        private readonly string[] _persianLastNames = new[]
        {
            "احمدی", "محمدی", "حسینی", "رضایی", "کریمی", "نوری", "صادقی", "مرادی",
            "اکبری", "جعفری", "کاظمی", "موسوی", "علیپور", "حسن‌پور", "رحمانی", "یوسفی",
            "فتحی", "باقری", "قاسمی", "شریفی", "فروغی", "امینی", "سلیمانی", "هاشمی"
        };

        private readonly Random _random = new Random(12345); // Deterministic seed

        public StaffSeeder(
            ServiceCatalogDbContext context,
            ILogger<StaffSeeder> logger)
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

                _logger.LogInformation("Adding Iranian staff to {Count} providers", providers.Count);

                var totalStaffAdded = 0;

              

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added {TotalStaff} Iranian staff members to {ProviderCount} providers",
                    totalStaffAdded, providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian staff data");
                throw;
            }
        }


        private (string FirstName, string LastName) GetRandomPersianName(bool isMale)
        {
            var firstName = isMale
                ? _persianMaleFirstNames[_random.Next(_persianMaleFirstNames.Length)]
                : _persianFemaleFirstNames[_random.Next(_persianFemaleFirstNames.Length)];

            var lastName = _persianLastNames[_random.Next(_persianLastNames.Length)];

            return (firstName, lastName);
        }

        private PhoneNumber GenerateIranianPhone()
        {
            // Generate Iranian mobile number format: 09XXXXXXXXX
            var number = $"09{_random.Next(10, 99)}{_random.Next(1000000, 9999999)}";
            return PhoneNumber.From(number);
        }
    }
}
