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
                    .Include(p => p.Staff)
                    .Where(p => !p.Staff.Any())
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogInformation("All providers already have staff. Skipping...");
                    return;
                }

                _logger.LogInformation("Adding Iranian staff to {Count} providers", providers.Count);

                var totalStaffAdded = 0;

                foreach (var provider in providers)
                {
                    var staffCount = AddStaffToProvider(provider);
                    totalStaffAdded += staffCount;
                }

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

        private int AddStaffToProvider(Domain.Aggregates.Provider provider)
        {
            var staffCount = 0;

            switch (provider.ProviderType)
            {
                case ProviderType.Individual:
                    // Individual barbershop/salon has owner only
                    var (ownerFirstName, ownerLastName) = GetRandomPersianName(true);
                    provider.AddStaff(
                        ownerFirstName,
                        ownerLastName,
                        StaffRole.Owner,
                        GenerateIranianPhone());
                    staffCount = 1;
                    break;

                case ProviderType.Salon:
                    // Salon has owner and 3-5 female staff
                    var (salonOwnerFirst, salonOwnerLast) = GetRandomPersianName(false);
                    provider.AddStaff(
                        salonOwnerFirst,
                        salonOwnerLast,
                        StaffRole.Owner,
                        GenerateIranianPhone());

                    for (int i = 0; i < 4; i++)
                    {
                        var (firstName, lastName) = GetRandomPersianName(false);
                        var role = i == 3 ? StaffRole.Receptionist : StaffRole.ServiceProvider;
                        provider.AddStaff(
                            firstName,
                            lastName,
                            role,
                            GenerateIranianPhone());
                    }
                    staffCount = 5;
                    break;

                case ProviderType.Spa:
                    // Spa has mixed staff with therapists
                    var (spaOwnerFirst, spaOwnerLast) = GetRandomPersianName(false);
                    provider.AddStaff(
                        spaOwnerFirst,
                        spaOwnerLast,
                        StaffRole.Owner,
                        GenerateIranianPhone());

                    // Add 3 therapists (mixed gender)
                    for (int i = 0; i < 3; i++)
                    {
                        var (firstName, lastName) = GetRandomPersianName(i % 2 == 0);
                        provider.AddStaff(
                            firstName,
                            lastName,
                            StaffRole.ServiceProvider,
                            GenerateIranianPhone());
                    }

                    // Add receptionist
                    var (recFirst, recLast) = GetRandomPersianName(false);
                    provider.AddStaff(
                        recFirst,
                        recLast,
                        StaffRole.Receptionist,
                        GenerateIranianPhone());

                    staffCount = 5;
                    break;

                case ProviderType.Medical:
                case ProviderType.Clinic:
                    // Medical/Clinic has doctor, nurses
                    var (doctorFirst, doctorLast) = GetRandomPersianName(true);
                    provider.AddStaff(
                        $"دکتر {doctorFirst}",
                        doctorLast,
                        StaffRole.Specialist,
                        GenerateIranianPhone());

                    // Add 2 nurses
                    for (int i = 0; i < 2; i++)
                    {
                        var (nurseFirst, nurseLast) = GetRandomPersianName(false);
                        provider.AddStaff(
                            nurseFirst,
                            nurseLast,
                            StaffRole.ServiceProvider,
                            GenerateIranianPhone());
                    }

                    staffCount = 3;
                    break;

                case ProviderType.GymFitness:
                    // Gym has trainers
                    var (gymOwnerFirst, gymOwnerLast) = GetRandomPersianName(true);
                    provider.AddStaff(
                        $"مربی {gymOwnerFirst}",
                        gymOwnerLast,
                        StaffRole.Owner,
                        GenerateIranianPhone());

                    for (int i = 0; i < 3; i++)
                    {
                        var (trainerFirst, trainerLast) = GetRandomPersianName(i % 2 == 0);
                        provider.AddStaff(
                            $"مربی {trainerFirst}",
                            trainerLast,
                            StaffRole.ServiceProvider,
                            GenerateIranianPhone());
                    }

                    staffCount = 4;
                    break;

                case ProviderType.Professional:
                    // Professional service has specialists
                    var (profOwnerFirst, profOwnerLast) = GetRandomPersianName(false);
                    provider.AddStaff(
                        profOwnerFirst,
                        profOwnerLast,
                        StaffRole.Owner,
                        GenerateIranianPhone());

                    for (int i = 0; i < 2; i++)
                    {
                        var (specialistFirst, specialistLast) = GetRandomPersianName(i % 2 == 0);
                        provider.AddStaff(
                            specialistFirst,
                            specialistLast,
                            StaffRole.Specialist,
                            GenerateIranianPhone());
                    }

                    staffCount = 3;
                    break;

                default:
                    var (defaultFirst, defaultLast) = GetRandomPersianName(true);
                    provider.AddStaff(
                        defaultFirst,
                        defaultLast,
                        StaffRole.Owner,
                        GenerateIranianPhone());
                    staffCount = 1;
                    break;
            }

            return staffCount;
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
            return PhoneNumber.Create(number);
        }
    }
}
