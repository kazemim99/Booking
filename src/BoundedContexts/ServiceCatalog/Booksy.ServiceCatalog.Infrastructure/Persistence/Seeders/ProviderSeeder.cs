using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds Iranian/Persian providers (salons, spas, clinics) across major Iranian cities
    /// </summary>
    public sealed class ProviderSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ProviderSeeder> _logger;

        public ProviderSeeder(
            ServiceCatalogDbContext context,
            ILogger<ProviderSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Providers.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Providers already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian providers seeding...");

                var providers = GetIranianProviders();
                await _context.Providers.AddRangeAsync(providers, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} Iranian providers", providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian providers");
                throw;
            }
        }

        private List<Provider> GetIranianProviders()
        {
            return new List<Provider>
            {
                // Tehran Providers
                CreateProvider(
                    "آرایشگاه زیبای پارسی",
                    "Arayeshgah Ziba Parsi",
                    "آرایشگاه تخصصی بانوان با خدمات کامل زیبایی",
                    ProviderType.Salon,
                    "Tehran",
                    "Vanak",
                    "info@zibaparsi.ir",
                    "02188776655",
                    "Sara",
                    "Rezaei"),

                CreateProvider(
                    "سالن زیبایی نگین",
                    "Negin Beauty Salon",
                    "مرکز تخصصی پوست، مو و زیبایی",
                    ProviderType.Salon,
                    "Tehran",
                    "Saadat Abad",
                    "contact@neginbeauty.ir",
                    "02188123456",
                    "Mina",
                    "Karimi"),

                CreateProvider(
                    "آرایشگاه مردانه آریا",
                    "Arya Barbershop",
                    "آرایشگاه مردانه با سبک مدرن و سنتی",
                    ProviderType.Individual,
                    "Tehran",
                    "Jordan",
                    "info@aryabarbershop.ir",
                    "02188234567",
                    "Reza",
                    "Mohammadi"),

                CreateProvider(
                    "اسپا و ماساژ درمانی کوهسار",
                    "Koohsar Spa & Therapeutic Massage",
                    "مرکز تخصصی ماساژ درمانی و اسپا",
                    ProviderType.Spa,
                    "Tehran",
                    "Farmanieh",
                    "info@koohsarspa.ir",
                    "02188345678",
                    "Amir",
                    "Hosseini"),

                // Mashhad Providers
                CreateProvider(
                    "کلینیک زیبایی مهر",
                    "Mehr Beauty Clinic",
                    "کلینیک تخصصی پوست و مو با پزشکان مجرب",
                    ProviderType.Clinic,
                    "Mashhad",
                    "Ahmadabad",
                    "info@mehrclinic.ir",
                    "05138112233",
                    "Dr. Maryam",
                    "Abbasi"),

                CreateProvider(
                    "سالن زیبایی گلستان",
                    "Golestan Beauty Center",
                    "ارائه خدمات زیبایی و آرایشی با کیفیت بالا",
                    ProviderType.Salon,
                    "Mashhad",
                    "Koohsangi",
                    "contact@golestanbeauty.ir",
                    "05138223344",
                    "Zahra",
                    "Gholami"),

                // Isfahan Providers
                CreateProvider(
                    "آرایشگاه زرین کوب",
                    "Zarrin Koob Salon",
                    "آرایشگاه سنتی و مدرن در قلب اصفهان",
                    ProviderType.Salon,
                    "Isfahan",
                    "Enghelab",
                    "info@zarrinkoob.ir",
                    "03136334455",
                    "Fatemeh",
                    "Bahrami"),

                CreateProvider(
                    "اسپا سیمرغ",
                    "Simorgh Spa",
                    "مرکز اسپا و ماساژ با تجهیزات مدرن",
                    ProviderType.Spa,
                    "Isfahan",
                    "Shahrak Azadi",
                    "info@simorghspa.ir",
                    "03136445566",
                    "Hassan",
                    "Sadeghi"),

                // Shiraz Providers
                CreateProvider(
                    "سالن زیبایی پریناز",
                    "Parinaz Beauty Salon",
                    "سالن زیبایی مجهز به تکنولوژی روز",
                    ProviderType.Salon,
                    "Shiraz",
                    "Chamran",
                    "info@parinazbeauty.ir",
                    "07138556677",
                    "Parinaz",
                    "Jafari"),

                CreateProvider(
                    "آرایشگاه مردانه پارس",
                    "Pars Men's Barbershop",
                    "آرایشگاه مردانه با طراحی مدرن",
                    ProviderType.Individual,
                    "Shiraz",
                    "Zand",
                    "info@parsbarbershop.ir",
                    "07138667788",
                    "Ali",
                    "Rahimi"),

                // Tabriz Providers
                CreateProvider(
                    "کلینیک تخصصی آذر",
                    "Azar Specialty Clinic",
                    "کلینیک زیبایی با پزشکان متخصص",
                    ProviderType.Clinic,
                    "Tabriz",
                    "Valiasr",
                    "info@azarclinic.ir",
                    "04138778899",
                    "Dr. Leila",
                    "Ahmadi"),

                CreateProvider(
                    "سالن زیبایی شاهدخت",
                    "Shahdokht Beauty Center",
                    "مرکز جامع خدمات زیبایی بانوان",
                    ProviderType.Salon,
                    "Tabriz",
                    "Elgoli",
                    "contact@shahdokht.ir",
                    "04138889900",
                    "Nazanin",
                    "Moradi"),

                // Karaj Providers
                CreateProvider(
                    "اسپا و سلامت البرز",
                    "Alborz Health & Spa",
                    "مرکز سلامت و تندرستی با اسپای لوکس",
                    ProviderType.Spa,
                    "Karaj",
                    "Gohardasht",
                    "info@alborzspa.ir",
                    "02632990011",
                    "Mehdi",
                    "Rahmani"),

                CreateProvider(
                    "آرایشگاه مهسا",
                    "Mahsa Hair Salon",
                    "آرایشگاه بانوان با کادر حرفه‌ای",
                    ProviderType.Salon,
                    "Karaj",
                    "Rajaee Shahr",
                    "info@mahsasalon.ir",
                    "02632001122",
                    "Mahsa",
                    "Salehi"),

                // Qom Providers
                CreateProvider(
                    "سالن زیبایی فاطمه",
                    "Fatemeh Beauty Salon",
                    "سالن زیبایی با رعایت موازین اسلامی",
                    ProviderType.Salon,
                    "Qom",
                    "Moalem",
                    "info@fatemehsalon.ir",
                    "02538112233",
                    "Fatemeh",
                    "Kazemi"),

                // Ahvaz Providers
                CreateProvider(
                    "کلینیک پوست و مو کارون",
                    "Karoun Skin & Hair Clinic",
                    "کلینیک تخصصی درمان‌های پوستی",
                    ProviderType.Clinic,
                    "Ahvaz",
                    "Kianpars",
                    "info@karounclinic.ir",
                    "06138223344",
                    "Dr. Saeed",
                    "Najafi"),

                CreateProvider(
                    "باشگاه بدنسازی پارسیان",
                    "Parsian Fitness Club",
                    "باشگاه ورزشی با مربیان مجرب",
                    ProviderType.GymFitness,
                    "Tehran",
                    "Niavaran",
                    "info@parsianfitness.ir",
                    "02188998877",
                    "Mohammad",
                    "Azizi"),

                CreateProvider(
                    "مرکز ماساژ درمانی سپید",
                    "Sepid Therapeutic Massage Center",
                    "مرکز تخصصی فیزیوتراپی و ماساژ",
                    ProviderType.Professional,
                    "Tehran",
                    "Elahieh",
                    "info@sepidmassage.ir",
                    "02188776644",
                    "Saman",
                    "Rostami"),

                CreateProvider(
                    "سالن زیبایی یاس",
                    "Yas Beauty Parlor",
                    "خدمات کامل آرایش عروس و مهمانی",
                    ProviderType.Salon,
                    "Kerman",
                    "Azadi Square",
                    "info@yasbeauty.ir",
                    "03438334455",
                    "Yasamin",
                    "Hashemi"),

                CreateProvider(
                    "آرایشگاه مردانه شاهین",
                    "Shahin Men's Grooming",
                    "آرایشگاه مردانه VIP",
                    ProviderType.Individual,
                    "Rasht",
                    "Golsar",
                    "info@shahingroom.ir",
                    "01338445566",
                    "Shahin",
                    "Farahani")
            };
        }

        private Provider CreateProvider(
            string persianName,
            string englishName,
            string description,
            ProviderType type,
            string city,
            string district,
            string email,
            string phone,
            string ownerFirstName,
            string ownerLastName)
        {
            var ownerId = UserId.From(Guid.NewGuid());
            var emailValue = Email.Create(email);
            var phoneValue = PhoneNumber.From(phone);

            var contactInfo = ContactInfo.Create(
                emailValue,
                phoneValue,
                null,
                $"https://www.{englishName.Replace(" ", "").Replace("'", "").ToLower()}.ir");

            var address = BusinessAddress.Create(
                $"{district}, {city}, Iran",
                district,
                city,
                city, // State/Province
                "00000",
                "Iran",
                null,
                null,
                35.6892, // Default Tehran coordinates
                51.3890);

            var provider = Provider.CreateDraft(
                ownerId,
                ownerFirstName,
                ownerLastName,
                $"{englishName} - {persianName}",
                description,
                type,
                contactInfo,
                address,
                ProviderHierarchyType.Organization,
                registrationStep: 9);

            // Complete registration and activate the provider for seeding
            provider.CompleteRegistration();
            provider.Activate();

            return provider;
        }
    }
}
