using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Testing.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds Iranian/Persian customer users for the system
    /// </summary>
    public sealed class CustomerSeeder : ISeeder
    {
        private readonly UserManagementDbContext _context;
        private readonly ILogger<CustomerSeeder> _logger;

        // Persian male first names
        private readonly string[] _malePersianFirstNames = new[]
        {
            "علی", "رضا", "محمد", "حسین", "امیر", "مهدی", "سعید", "مسعود",
            "فرهاد", "کامران", "بهروز", "داریوش", "کیوان", "پیمان", "آرش", "سهراب",
            "کاوه", "بابک", "آرمان", "پوریا", "سامان", "سینا", "فرشاد", "شاهین"
        };

        // Persian female first names
        private readonly string[] _femalePersianFirstNames = new[]
        {
            "فاطمه", "زهرا", "مریم", "سارا", "نگار", "لیلا", "نیلوفر", "شیرین",
            "مهناز", "پریسا", "نازنین", "پریناز", "شیدا", "آناهیتا", "طاها", "سمانه",
            "ملیکا", "نیکی", "دریا", "ستاره", "مهسا", "الهه", "مینا", "نرگس"
        };

        // Persian last names
        private readonly string[] _persianLastNames = new[]
        {
            "احمدی", "محمدی", "حسینی", "رضایی", "کریمی", "نوری", "صادقی", "مرادی",
            "اکبری", "جعفری", "کاظمی", "موسوی", "علیپور", "حسن‌پور", "رحمانی", "یوسفی",
            "فتحی", "باقری", "قاسمی", "شریفی", "فروغی", "امینی", "سلیمانی", "هاشمی",
            "نصیری", "زارعی", "کامکار", "رستمی", "عسکری", "پارسا", "فرهادی", "کیانی"
        };

        private readonly Random _random = new Random(54321); // Deterministic seed

        public CustomerSeeder(
            UserManagementDbContext context,
            ILogger<CustomerSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Users.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Users already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian customer users seeding...");

                var users = GenerateIranianCustomers(50); // Generate 50 customers

                await _context.Users.AddRangeAsync(users, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} Iranian customer users", users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian customer users");
                throw;
            }
        }

        private List<User> GenerateIranianCustomers(int count)
        {
            var users = new List<User>();

            for (int i = 0; i < count; i++)
            {
                var isMale = _random.Next(2) == 0;
                var (firstName, lastName) = GetRandomPersianName(isMale);
                var gender = isMale ? "Male" : "Female";

                var email = GenerateEmail(firstName, lastName, i);
                var phoneNumber = GenerateIranianPhone();

                // Create user profile with all details
                var dateOfBirth = GenerateRandomDateOfBirth();
                var profile = UserProfile.Create(
                    firstName,
                    lastName,
                    null, // middleName
                    dateOfBirth,
                    gender); // gender as string

                // Add phone number to profile
                profile.UpdateContactInfo(phoneNumber, null, null);

                // Build user using the builder (for testing/seeding)
                var user = UserBuilder.Create()
                    .WithEmail(email)
                    .WithPassword("Pass@123456") // Default password for seeding
                    .WithFullProfile(profile) // Use the complete profile
                    .WithType(UserType.Customer)
                    .AsActive() // Pre-activated for testing (use AsActive, not AsActivated)
                    .Build();

                users.Add(user);
            }

            return users;
        }

        private (string FirstName, string LastName) GetRandomPersianName(bool isMale)
        {
            var firstName = isMale
                ? _malePersianFirstNames[_random.Next(_malePersianFirstNames.Length)]
                : _femalePersianFirstNames[_random.Next(_femalePersianFirstNames.Length)];

            var lastName = _persianLastNames[_random.Next(_persianLastNames.Length)];

            return (firstName, lastName);
        }

        private string GenerateEmail(string firstName, string lastName, int index)
        {
            // Generate email using transliterated names
            var emailName = $"{TransliteratePersian(firstName)}.{TransliteratePersian(lastName)}{index}";
            return $"{emailName}@example.ir".ToLower();
        }

        private PhoneNumber GenerateIranianPhone()
        {
            // Generate Iranian mobile number format: 09XXXXXXXXX
            var number = $"09{_random.Next(10, 99)}{_random.Next(1000000, 9999999)}";
            return PhoneNumber.Create(number);
        }

        private DateTime GenerateRandomDateOfBirth()
        {
            // Generate ages between 18 and 65
            var yearsAgo = _random.Next(18, 66);
            var monthsAgo = _random.Next(0, 12);
            var daysAgo = _random.Next(0, 28);

            return DateTime.UtcNow
                .AddYears(-yearsAgo)
                .AddMonths(-monthsAgo)
                .AddDays(-daysAgo);
        }

        private string TransliteratePersian(string persianText)
        {
            // Simple transliteration map for common Persian names
            var transliterationMap = new Dictionary<string, string>
            {
                {"علی", "ali"}, {"رضا", "reza"}, {"محمد", "mohammad"}, {"حسین", "hossein"},
                {"امیر", "amir"}, {"مهدی", "mehdi"}, {"سعید", "saeed"}, {"مسعود", "masoud"},
                {"فرهاد", "farhad"}, {"کامران", "kamran"}, {"بهروز", "behrooz"}, {"داریوش", "dariush"},
                {"کیوان", "keyvan"}, {"پیمان", "peyman"}, {"آرش", "arash"}, {"سهراب", "sohrab"},
                {"کاوه", "kaveh"}, {"بابک", "babak"}, {"آرمان", "arman"}, {"پوریا", "pouria"},
                {"سامان", "saman"}, {"سینا", "sina"}, {"فرشاد", "farshad"}, {"شاهین", "shahin"},

                {"فاطمه", "fatemeh"}, {"زهرا", "zahra"}, {"مریم", "maryam"}, {"سارا", "sara"},
                {"نگار", "negar"}, {"لیلا", "leila"}, {"نیلوفر", "niloofar"}, {"شیرین", "shirin"},
                {"مهناز", "mahnaz"}, {"پریسا", "parisa"}, {"نازنین", "nazanin"}, {"پریناز", "parinaz"},
                {"شیدا", "sheyda"}, {"آناهیتا", "anahita"}, {"طاها", "taha"}, {"سمانه", "samaneh"},
                {"ملیکا", "melika"}, {"نیکی", "niki"}, {"دریا", "darya"}, {"ستاره", "setareh"},
                {"مهسا", "mahsa"}, {"الهه", "elaheh"}, {"مینا", "mina"}, {"نرگس", "narges"},

                {"احمدی", "ahmadi"}, {"محمدی", "mohammadi"}, {"حسینی", "hosseini"}, {"رضایی", "rezaei"},
                {"کریمی", "karimi"}, {"نوری", "noori"}, {"صادقی", "sadeghi"}, {"مرادی", "moradi"},
                {"اکبری", "akbari"}, {"جعفری", "jafari"}, {"کاظمی", "kazemi"}, {"موسوی", "mousavi"},
                {"علیپور", "alipour"}, {"حسن‌پور", "hassanpour"}, {"رحمانی", "rahmani"}, {"یوسفی", "yousefi"},
                {"فتحی", "fathi"}, {"باقری", "bagheri"}, {"قاسمی", "ghasemi"}, {"شریفی", "sharifi"},
                {"فروغی", "foroughi"}, {"امینی", "amini"}, {"سلیمانی", "soleimani"}, {"هاشمی", "hashemi"},
                {"نصیری", "nasiri"}, {"زارعی", "zarei"}, {"کامکار", "kamkar"}, {"رستمی", "rostami"},
                {"عسکری", "askari"}, {"پارسا", "parsa"}, {"فرهادی", "farhadi"}, {"کیانی", "kiani"}
            };

            return transliterationMap.TryGetValue(persianText, out var transliterated)
                ? transliterated
                : persianText.ToLower();
        }
    }
}
