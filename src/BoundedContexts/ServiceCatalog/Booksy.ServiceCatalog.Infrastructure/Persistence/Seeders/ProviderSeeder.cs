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

        /// <summary>
        /// Seed providers for <b>Parsabad Moghan (پارس‌آباد مغان)</b>, Ardabil province — the first city the
        /// product goes to market in, and therefore the city discovery has to work correctly in.
        ///
        /// <para>This replaces a seed set with two problems. It scattered providers across ten cities the product
        /// does not launch in, and — because <c>CreateProvider</c> hard-coded Tehran's coordinates for every row —
        /// every provider sat on one identical point. "Near me", radius filtering and the map could not be
        /// exercised at all: distance was either zero or meaningless across the whole catalogue.</para>
        ///
        /// <para>Each provider below carries its own coordinates, spread over roughly 4 km of the real town, so
        /// nearest-first ordering, radius cut-offs and map pin separation all produce observable results. The
        /// spread is deliberate: several pairs sit close together to exercise tie-breaking and overlapping pins,
        /// while the outliers sit far enough out to fall outside a small radius.</para>
        /// </summary>
        private List<Provider> GetIranianProviders()
        {
            // Parsabad Moghan town centre is approximately 39.6482 N, 47.9174 E.
            return new List<Provider>
            {
                CreateProvider(
                    "آرایشگاه مردانه شهریار",
                    "Shahriar Barbershop",
                    "اصلاح و آرایش مو و ریش آقایان با جدیدترین متدهای روز",
                    ServiceCategory.Barbershop,
                    "خیابان امام خمینی",
                    "shahriar.barber@booksy.ir",
                    "09141110001",
                    "شهریار",
                    "احمدی",
                    39.6491, 47.9182,
                    "https://images.unsplash.com/photo-1503951914875-452162b0f3f1?w=800&q=80"),

                CreateProvider(
                    "سالن زیبایی نگین مغان",
                    "Negin Moghan Beauty Salon",
                    "خدمات تخصصی پوست، مو، ناخن و آرایش عروس",
                    ServiceCategory.BeautySalon,
                    "خیابان طالقانی",
                    "negin.moghan@booksy.ir",
                    "09141110002",
                    "نگین",
                    "رستمی",
                    39.6503, 47.9161,
                    "https://images.unsplash.com/photo-1560066984-138dadb4c035?w=800&q=80"),

                CreateProvider(
                    "ناخن‌کده آرام",
                    "Aram Nail Studio",
                    "کاشت، ترمیم و طراحی ناخن با مواد درجه یک",
                    ServiceCategory.NailSalon,
                    "خیابان شهید بهشتی",
                    "aram.nails@booksy.ir",
                    "09141110003",
                    "آرام",
                    "موسوی",
                    39.6467, 47.9203,
                    "https://images.unsplash.com/photo-1604654894610-df63bc536371?w=800&q=80"),

                CreateProvider(
                    "اسپا و ماساژ آرامش",
                    "Aramesh Spa",
                    "ماساژ تخصصی، سنگ داغ و برنامه‌های آرام‌سازی",
                    ServiceCategory.Spa,
                    "بلوار ولیعصر",
                    "aramesh.spa@booksy.ir",
                    "09141110004",
                    "سحر",
                    "قاسمی",
                    39.6528, 47.9218,
                    "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&q=80"),

                CreateProvider(
                    "ماساژ درمانی سلامت",
                    "Salamat Massage Therapy",
                    "ماساژ درمانی، ورزشی و تسکین دردهای عضلانی",
                    ServiceCategory.Massage,
                    "خیابان مطهری",
                    "salamat.massage@booksy.ir",
                    "09141110005",
                    "بابک",
                    "نجفی",
                    39.6455, 47.9139,
                    "https://images.unsplash.com/photo-1600334089648-b0d9d3028eb2?w=800&q=80"),

                CreateProvider(
                    "باشگاه بدنسازی پارس",
                    "Pars Fitness Club",
                    "سالن بدنسازی مجهز با مربیان حرفه‌ای و برنامه تمرینی شخصی",
                    ServiceCategory.Gym,
                    "شهرک ولیعصر",
                    "pars.gym@booksy.ir",
                    "09141110006",
                    "رضا",
                    "علیزاده",
                    39.6572, 47.9245,
                    "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800&q=80"),

                CreateProvider(
                    "خانه یوگا مهر",
                    "Mehr Yoga House",
                    "کلاس‌های یوگا، تنفس و مدیتیشن برای همه سطوح",
                    ServiceCategory.Yoga,
                    "خیابان فردوسی",
                    "mehr.yoga@booksy.ir",
                    "09141110007",
                    "مهسا",
                    "کریمی",
                    39.6438, 47.9098,
                    "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800&q=80"),

                CreateProvider(
                    "کلینیک پوست و مو مغان",
                    "Moghan Skin Clinic",
                    "خدمات تخصصی پوست، لیزر و جوان‌سازی زیر نظر پزشک",
                    ServiceCategory.MedicalClinic,
                    "خیابان امام خمینی",
                    "moghan.clinic@booksy.ir",
                    "09141110008",
                    "لیلا",
                    "حسینی",
                    39.6486, 47.9176,
                    "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&q=80"),

                CreateProvider(
                    "دندانپزشکی لبخند",
                    "Labkhand Dental",
                    "دندانپزشکی زیبایی، ترمیمی و بهداشت دهان و دندان",
                    ServiceCategory.Dental,
                    "خیابان شریعتی",
                    "labkhand.dental@booksy.ir",
                    "09141110009",
                    "امیر",
                    "صادقی",
                    39.6512, 47.9134,
                    "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?w=800&q=80"),

                CreateProvider(
                    "فیزیوتراپی توان",
                    "Tavan Physiotherapy",
                    "توانبخشی، فیزیوتراپی و درمان آسیب‌های ورزشی",
                    ServiceCategory.Physiotherapy,
                    "بلوار معلم",
                    "tavan.physio@booksy.ir",
                    "09141110010",
                    "سعید",
                    "زارعی",
                    39.6399, 47.9231,
                    "https://images.unsplash.com/photo-1519494026892-80bbd2d6fd0d?w=800&q=80"),

                CreateProvider(
                    "سالن زیبایی گلستان",
                    "Golestan Beauty Salon",
                    "آرایش، شینیون و خدمات کامل عروس",
                    ServiceCategory.BeautySalon,
                    "خیابان طالقانی",
                    "golestan.beauty@booksy.ir",
                    "09141110011",
                    "گلناز",
                    "یوسفی",
                    39.6506, 47.9158,
                    "https://images.unsplash.com/photo-1595476108010-b4d1f102b1b1?w=800&q=80"),

                CreateProvider(
                    "آرایشگاه مردانه آریا",
                    "Arya Barbershop",
                    "پیرایش مردانه، اصلاح صورت و خدمات ویژه دامادی",
                    ServiceCategory.Barbershop,
                    "خیابان سعدی",
                    "arya.barber@booksy.ir",
                    "09141110012",
                    "آرش",
                    "مرادی",
                    39.6624, 47.9302,
                    "https://images.unsplash.com/photo-1585747860715-2ba37e788b70?w=800&q=80"),
            };
        }

        /// <param name="latitude">
        /// Real coordinates for this specific business. Previously every seeded provider was given Tehran's
        /// centre, which made distance sorting, radius filtering and the map meaningless — all pins landed on
        /// one another. Each provider now sits where it actually is.
        /// </param>
        /// <param name="profileImageUrl">
        /// Storefront image. Without one the catalogue rendered as rows of identical grey placeholders, which
        /// made the list impossible to scan and hid whether image loading worked at all.
        /// </param>
        private Provider CreateProvider(
            string persianName,
            string englishName,
            string description,
            ServiceCategory type,
            string street,
            string email,
            string phone,
            string ownerFirstName,
            string ownerLastName,
            double latitude,
            double longitude,
            string profileImageUrl)
        {
            const string City = "پارس‌آباد";
            const string Province = "اردبیل";

            var ownerId = UserId.From(Guid.NewGuid());
            var emailValue = Email.Create(email);
            var phoneValue = PhoneNumber.From(phone);

            var contactInfo = ContactInfo.Create(
                emailValue,
                phoneValue,
                null,
                $"https://www.{englishName.Replace(" ", "").Replace("'", "").ToLower()}.ir");

            var address = BusinessAddress.Create(
                $"{street}، {City}، {Province}، ایران",
                street,
                City,
                Province,
                "5691",
                "ایران",
                null,
                null,
                latitude,
                longitude);

            // The Persian name leads: this is what a customer in Parsabad reads and searches for. The previous
            // "English - Persian" order buried it behind a transliteration nobody types.
            var displayName = persianName;

            var provider = Provider.CreateDraft(
                ownerId,
                ownerFirstName,
                ownerLastName,
                displayName,
                description,
                type,
                contactInfo,
                address,
                ProviderHierarchyType.Organization,
                registrationStep: 9);

            provider.UpdateBusinessProfile(displayName, description, profileImageUrl);

            // Complete registration and activate the provider for seeding
            provider.CompleteRegistration();
            provider.Activate();

            return provider;
        }
    }
}
