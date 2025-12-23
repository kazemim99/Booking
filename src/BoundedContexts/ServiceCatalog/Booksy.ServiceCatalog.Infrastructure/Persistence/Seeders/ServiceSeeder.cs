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
    /// Seeds Iranian/Persian beauty and wellness services
    /// </summary>
    public sealed class ServiceSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceSeeder> _logger;

        public ServiceSeeder(
            ServiceCatalogDbContext context,
            ILogger<ServiceSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Services.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Services already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian services seeding...");

                var providers = await _context.Providers.ToListAsync(cancellationToken);
                var services = new List<Service>();

                foreach (var provider in providers)
                {
                    services.AddRange(CreateServicesForProvider(provider));
                }

                await _context.Services.AddRangeAsync(services, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} Iranian services for {ProviderCount} providers",
                    services.Count, providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian services");
                throw;
            }
        }

        private List<Service> CreateServicesForProvider(Provider provider)
        {
            var services = new List<Service>();
            var baseServices = GetServicesByProviderType(provider.PrimaryCategory);

            foreach (var (persianName, englishName, description, price, duration, category) in baseServices)
            {
                var serviceCategory = ServiceCategory.Create(category, $"خدمات {category}");
                var priceValue = Price.Create(price, "IRR"); // Iranian Rial
                var durationValue = Duration.FromMinutes(duration);

                var service = Service.Create(
                    provider.Id,
                    $"{englishName} - {persianName}",
                    description,
                    serviceCategory,
                    ServiceType.Standard,
                    priceValue,
                    durationValue);

                services.Add(service);
            }

            return services;
        }

        private List<(string PersianName, string EnglishName, string Description, decimal Price, int Duration, string Category)>
            GetServicesByProviderType(ServiceCategory category)
        {
            return category switch
            {
                ServiceCategory.Barbershop => new List<(string, string, string, decimal, int, string)>
                {
                    ("کوتاهی مو مردانه", "Men's Haircut", "اصلاح و آرایش موی مردانه", 500000m, 30, "آرایشگری"),
                    ("اصلاح صورت", "Face Shave", "اصلاح صورت با تیغ و حوله داغ", 300000m, 20, "آرایشگری"),
                    ("اصلاح ریش", "Beard Trim", "اصلاح و فرم‌دهی ریش", 350000m, 25, "آرایشگری"),
                    ("رنگ مو", "Hair Color", "رنگ کردن موی سر", 800000m, 60, "آرایشگری")
                },

                ServiceCategory.HairSalon => new List<(string, string, string, decimal, int, string)>
                {
                    ("کوتاهی و فشن", "Cut & Style", "کوتاهی و مدل موی بانوان", 1200000m, 60, "آرایش و زیبایی"),
                    ("رنگ مو", "Hair Coloring", "رنگ کامل موی سر", 2500000m, 120, "آرایش و زیبایی"),
                    ("هایلایت", "Highlights", "هایلایت و مش مو", 2000000m, 90, "آرایش و زیبایی"),
                    ("کراتینه مو", "Keratin Treatment", "کراتینه و صاف کردن مو", 3500000m, 180, "مراقبت مو"),
                    ("مانیکور", "Manicure", "مانیکور و زیبایی ناخن", 600000m, 45, "زیبایی"),
                    ("پدیکور", "Pedicure", "پدیکور و مراقبت پا", 800000m, 60, "زیبایی"),
                    ("آرایش عروس", "Bridal Makeup", "آرایش کامل عروس", 5000000m, 120, "آرایش"),
                    ("آرایش مهمانی", "Party Makeup", "آرایش ویژه مهمانی", 2000000m, 60, "آرایش"),
                    ("اپیلاسیون صورت", "Face Threading", "بند انداختن ابرو و صورت", 400000m, 30, "زیبایی"),
                    ("اپیلاسیون بدن", "Body Waxing", "وکس کامل بدن", 1500000m, 90, "زیبایی")
                },

                ServiceCategory.Spa => new List<(string, string, string, decimal, int, string)>
                {
                    ("ماساژ سوئدی", "Swedish Massage", "ماساژ کامل بدن به سبک سوئدی", 1800000m, 60, "ماساژ"),
                    ("ماساژ بافت عمقی", "Deep Tissue Massage", "ماساژ درمانی بافت عمقی", 2200000m, 60, "ماساژ درمانی"),
                    ("ماساژ سنگ داغ", "Hot Stone Massage", "ماساژ با سنگ‌های گرم ولکانیکی", 2500000m, 75, "ماساژ"),
                    ("ماساژ آروماتراپی", "Aromatherapy Massage", "ماساژ با اسانس‌های طبیعی", 2000000m, 60, "ماساژ"),
                    ("پاکسازی پوست", "Facial Treatment", "پاکسازی و درمان پوست صورت", 1500000m, 75, "مراقبت پوست"),
                    ("میکرودرم", "Microdermabrasion", "لایه‌برداری و جوانسازی پوست", 2000000m, 60, "مراقبت پوست"),
                    ("ماسک طلا", "Gold Facial", "ماسک طلای 24 عیار", 3500000m, 90, "مراقبت پوست"),
                    ("بادی اسکراب", "Body Scrub", "لایه‌برداری بدن", 1200000m, 45, "مراقبت بدن"),
                    ("بادی رپ", "Body Wrap", "پیچش بدن برای سم‌زدایی", 2800000m, 90, "مراقبت بدن")
                },

                ServiceCategory.MedicalClinic => new List<(string, string, string, decimal, int, string)>
                {
                    ("لیزر موهای زائد", "Laser Hair Removal", "لیزر حذف موهای زائد", 1500000m, 45, "لیزر"),
                    ("لیزر جوانسازی", "Laser Rejuvenation", "جوانسازی پوست با لیزر", 3000000m, 60, "لیزر"),
                    ("مزوتراپی مو", "Hair Mesotherapy", "تزریق مزوتراپی برای رشد مو", 2500000m, 30, "درمان مو"),
                    ("مزوتراپی صورت", "Face Mesotherapy", "مزوتراپی جوانسازی صورت", 2000000m, 30, "جوانسازی"),
                    ("تزریق بوتاکس", "Botox Injection", "تزریق بوتاکس برای چین و چروک", 5000000m, 30, "زیبایی"),
                    ("تزریق فیلر", "Filler Injection", "تزریق فیلر برای حجم‌دهی", 4000000m, 30, "زیبایی"),
                    ("پی آر پی", "PRP Therapy", "درمان PRP برای مو و پوست", 3500000m, 45, "درمان"),
                    ("میکرونیدلینگ", "Microneedling", "میکرونیدلینگ برای بازسازی پوست", 2500000m, 60, "مراقبت پوست")
                },

                ServiceCategory.Dental => new List<(string, string, string, decimal, int, string)>
                {
                    ("کاشت مو", "Hair Transplant", "کاشت طبیعی مو به روش FIT/FUT", 15000000m, 240, "جراحی"),
                    ("عمل زیبایی بینی", "Rhinoplasty", "جراحی زیبایی بینی", 25000000m, 180, "جراحی"),
                    ("لیفت صورت", "Face Lift", "جراحی لیفت و جوانسازی صورت", 30000000m, 240, "جراحی"),
                    ("لیپوساکشن", "Liposuction", "جراحی لیپوساکشن برای چربی‌های موضعی", 20000000m, 180, "جراحی"),
                    ("درمان جوش", "Acne Treatment", "درمان تخصصی آکنه و جوش", 1500000m, 45, "درمان پوست"),
                    ("درمان لک", "Pigmentation Treatment", "درمان لک و کک و مک", 2000000m, 60, "درمان پوست")
                },

                ServiceCategory.Gym => new List<(string, string, string, decimal, int, string)>
                {
                    ("تمرین شخصی", "Personal Training", "جلسه تمرین با مربی شخصی", 1000000m, 60, "بدنسازی"),
                    ("تمرین گروهی", "Group Training", "کلاس تمرینی گروهی", 500000m, 60, "فیتنس"),
                    ("یوگا", "Yoga Class", "کلاس یوگا و مدیتیشن", 600000m, 60, "یوگا"),
                    ("پیلاتس", "Pilates", "کلاس پیلاتس", 700000m, 60, "پیلاتس"),
                    ("زومبا", "Zumba", "کلاس رقص زومبا", 500000m, 60, "آیروبیک"),
                    ("ماساژ ورزشی", "Sports Massage", "ماساژ بعد از ورزش", 1200000m, 45, "ماساژ")
                },

                ProviderType.Professional => new List<(string, string, string, decimal, int, string)>
                {
                    ("ماساژ فیزیوتراپی", "Physiotherapy Massage", "ماساژ درمانی توسط فیزیوتراپیست", 1500000m, 60, "فیزیوتراپی"),
                    ("طب سوزنی", "Acupuncture", "درمان با طب سوزنی", 1200000m, 45, "طب سنتی"),
                    ("طب سنتی ایرانی", "Persian Traditional Medicine", "مشاوره و درمان با طب سنتی", 1000000m, 30, "طب سنتی"),
                    ("رفلکسولوژی", "Reflexology", "ماساژ کف پا درمانی", 800000m, 45, "ماساژ درمانی"),
                    ("حجامت", "Cupping Therapy", "حجامت درمانی", 600000m, 30, "طب سنتی")
                },

                _ => new List<(string, string, string, decimal, int, string)>()
            };
        }
    }
}
