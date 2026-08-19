using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds the people who actually perform the work at each seeded provider.
    ///
    /// <para>Without this, a seeded catalogue is <b>unbookable</b>: the booking engine resolves a booking to a
    /// <em>bookable resource</em> (an active, service-providing <see cref="OrganizationMembership"/>), and
    /// <c>Service.Activate()</c> refuses to activate an organization's service that has no qualified staff. Zero
    /// memberships therefore meant 0 staff rows, every service stuck in <see cref="ServiceStatus.Draft"/>, and
    /// "no available times" on every provider in the app.</para>
    ///
    /// <para>Staff are seeded as <see cref="OrganizationMembership.CreateUnclaimed"/> memberships — the same
    /// mechanism a real salon uses to add a colleague who does not use the app yet. That is deliberate: the seeded
    /// providers' owner ids are synthetic, so there is no UserManagement person to link a membership to. An
    /// unclaimed membership is a first-class, bookable member identified by its <c>StaffProfile.DisplayName</c>,
    /// and it can later be claimed by a real account without losing its bookings.</para>
    /// </summary>
    public sealed class StaffSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<StaffSeeder> _logger;

        /// <summary>
        /// The roster for the seeded Parsabad Moghan businesses, keyed by the provider's contact e-mail — the only
        /// stable ASCII identity a seeded provider has (its display name is Persian and contains zero-width joiners).
        /// Names are gender- and trade-appropriate for each business, and the first entry of each roster is the
        /// owner named in <see cref="ProviderSeeder"/>, so the salon's staff list reads like the real business.
        /// </summary>
        private static readonly Dictionary<string, string[]> StaffByProviderEmail = new(StringComparer.OrdinalIgnoreCase)
        {
            // آرایشگاه مردانه شهریار — men's barbershop
            ["shahriar.barber@booksy.ir"] = new[] { "شهریار احمدی", "مهدی نوروزی", "یاسین قربانی" },

            // سالن زیبایی نگین مغان — women's beauty salon
            ["negin.moghan@booksy.ir"] = new[] { "نگین رستمی", "سمیرا اصلانی", "الهام پیرزاد" },

            // ناخن‌کده آرام — nail studio
            ["aram.nails@booksy.ir"] = new[] { "آرام موسوی", "فاطمه نصیری" },

            // اسپا و ماساژ آرامش — spa
            ["aramesh.spa@booksy.ir"] = new[] { "سحر قاسمی", "مینا دلیری", "حامد شکوری" },

            // ماساژ درمانی سلامت — massage therapy
            ["salamat.massage@booksy.ir"] = new[] { "بابک نجفی", "رقیه ولیزاده" },

            // باشگاه بدنسازی پارس — gym
            ["pars.gym@booksy.ir"] = new[] { "رضا علیزاده", "سینا محمدپور", "نگار بهرامی" },

            // خانه یوگا مهر — yoga house
            ["mehr.yoga@booksy.ir"] = new[] { "مهسا کریمی", "آیدا شریفی" },

            // کلینیک پوست و مو مغان — skin & hair clinic
            ["moghan.clinic@booksy.ir"] = new[] { "دکتر لیلا حسینی", "دکتر فرزاد عبدی", "نسترن رحیمی" },

            // دندانپزشکی لبخند — dental
            ["labkhand.dental@booksy.ir"] = new[] { "دکتر امیر صادقی", "دکتر سولماز بابایی" },

            // فیزیوتراپی توان — physiotherapy
            ["tavan.physio@booksy.ir"] = new[] { "سعید زارعی", "محسن اکبری" },

            // سالن زیبایی گلستان — beauty salon
            ["golestan.beauty@booksy.ir"] = new[] { "گلناز یوسفی", "پریسا خدایی", "شیدا مرادی" },

            // آرایشگاه مردانه آریا — men's barbershop
            ["arya.barber@booksy.ir"] = new[] { "آرش مرادی", "کامران سلیمانی" },
        };

        // Fallback pools, used only for a provider that is not in the roster above (e.g. one added later).
        private static readonly string[] PersianMaleFirstNames =
        {
            "علی", "رضا", "محمد", "حسین", "امیر", "مهدی", "سعید", "مسعود",
            "فرهاد", "کامران", "بهروز", "داریوش", "کیوان", "پیمان", "آرش", "سهراب"
        };

        private static readonly string[] PersianFemaleFirstNames =
        {
            "فاطمه", "زهرا", "مریم", "سارا", "نگار", "لیلا", "نیلوفر", "شیرین",
            "مهناز", "پریسا", "نازنین", "پریناز", "شیدا", "آناهیتا", "سمانه", "الهام"
        };

        private static readonly string[] PersianLastNames =
        {
            "احمدی", "محمدی", "حسینی", "رضایی", "کریمی", "نوری", "صادقی", "مرادی",
            "اکبری", "جعفری", "کاظمی", "موسوی", "علیپور", "رحمانی", "یوسفی",
            "فتحی", "باقری", "قاسمی", "شریفی", "امینی", "سلیمانی", "هاشمی"
        };

        private readonly Random _random = new(12345); // Deterministic seed — reproducible seeded data

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
                var providers = await _context.Providers.ToListAsync(cancellationToken);

                if (providers.Count == 0)
                {
                    _logger.LogWarning("No providers found. Skipping staff seeding.");
                    return;
                }

                // Per-provider idempotency: a provider that already has live members is left alone, so re-running
                // the seeder on an existing database tops up only the businesses that are still empty.
                var organizationsWithStaff = (await _context.OrganizationMemberships
                        .Where(m => m.Status != MembershipStatus.Terminated)
                        .Select(m => m.OrganizationId)
                        .ToListAsync(cancellationToken))
                    .Select(id => id.Value)
                    .ToHashSet();

                var toSeed = providers
                    .Where(p => !organizationsWithStaff.Contains(p.Id.Value))
                    .ToList();

                if (toSeed.Count == 0)
                {
                    _logger.LogInformation("All providers already have staff. Skipping...");
                    return;
                }

                var memberships = new List<OrganizationMembership>();

                foreach (var provider in toSeed)
                {
                    foreach (var displayName in GetStaffNamesFor(provider))
                    {
                        memberships.Add(OrganizationMembership.CreateUnclaimed(
                            provider.Id,
                            displayName,
                            providesServices: true));
                    }
                }

                await _context.OrganizationMemberships.AddRangeAsync(memberships, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully seeded {StaffCount} staff members across {ProviderCount} providers",
                    memberships.Count, toSeed.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding staff");
                throw;
            }
        }

        private IReadOnlyList<string> GetStaffNamesFor(Provider provider)
        {
            var email = provider.ContactInfo?.Email?.Value;

            if (email is not null && StaffByProviderEmail.TryGetValue(email, out var roster))
                return roster;

            return GenerateStaffNames(provider);
        }

        /// <summary>
        /// Fallback for a provider with no hand-written roster: 1–3 trade-appropriate Persian names, distinct
        /// within the business.
        /// </summary>
        private IReadOnlyList<string> GenerateStaffNames(Provider provider)
        {
            var count = _random.Next(1, 4);
            var names = new List<string>(count);

            for (var i = 0; i < count * 4 && names.Count < count; i++)
            {
                var male = PrefersMaleStaff(provider.PrimaryCategory) || (i % 2 == 1);
                var firstName = male
                    ? PersianMaleFirstNames[_random.Next(PersianMaleFirstNames.Length)]
                    : PersianFemaleFirstNames[_random.Next(PersianFemaleFirstNames.Length)];
                var lastName = PersianLastNames[_random.Next(PersianLastNames.Length)];

                var name = $"{firstName} {lastName}";
                if (!names.Contains(name))
                    names.Add(name);
            }

            return names;
        }

        private static bool PrefersMaleStaff(ServiceCategory category) =>
            category == ServiceCategory.Barbershop;
    }
}
