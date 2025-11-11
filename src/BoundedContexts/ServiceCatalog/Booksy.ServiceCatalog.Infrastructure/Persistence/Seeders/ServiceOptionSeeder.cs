using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds service options/add-ons with Iranian/Persian culture
    /// </summary>
    public sealed class ServiceOptionSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceOptionSeeder> _logger;
        private readonly Random _random = new Random(77889);

        public ServiceOptionSeeder(
            ServiceCatalogDbContext context,
            ILogger<ServiceOptionSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if any service already has options (owned entities are auto-included with OwnsMany)
                var hasOptions = await _context.Services
                    .AnyAsync(s => s.Options.Any(), cancellationToken);

                if (hasOptions)
                {
                    _logger.LogInformation("ServiceOptions already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian service options seeding...");

                // With OwnsMany, owned entities are automatically included - no need for explicit Include
                var services = await _context.Services
                    .ToListAsync(cancellationToken);

                if (!services.Any())
                {
                    _logger.LogWarning("No services found for option seeding.");
                    return;
                }

                var totalOptionsAdded = 0;

                foreach (var service in services)
                {
                    var optionsAdded = AddOptionsToService(service);
                    totalOptionsAdded += optionsAdded;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} service options for {ServiceCount} services",
                    totalOptionsAdded, services.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian service options");
                throw;
            }
        }

        private int AddOptionsToService(Domain.Aggregates.Service service)
        {
            var optionsAdded = 0;
            var serviceName = service.Name.ToLower();

            // Hair services options
            if (serviceName.Contains("کوتاهی") || serviceName.Contains("haircut") || serviceName.Contains("مو"))
            {
                service.AddOption(
                    "شستشوی مو - Hair Wash",
                    Price.Create(200000m, "IRR"),
                    Duration.FromMinutes(10));
                optionsAdded++;

                service.AddOption(
                    "ماساژ سر - Head Massage",
                    Price.Create(300000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;

                service.AddOption(
                    "سشوار - Blow Dry",
                    Price.Create(400000m, "IRR"),
                    Duration.FromMinutes(20));
                optionsAdded++;
            }

            // Color services options
            if (serviceName.Contains("رنگ") || serviceName.Contains("color") || serviceName.Contains("هایلایت"))
            {
                service.AddOption(
                    "ترمیم مو - Hair Treatment",
                    Price.Create(800000m, "IRR"),
                    Duration.FromMinutes(30));
                optionsAdded++;

                service.AddOption(
                    "کراتینه - Keratin",
                    Price.Create(1500000m, "IRR"),
                    Duration.FromMinutes(60));
                optionsAdded++;
            }

            // Massage services options
            if (serviceName.Contains("ماساژ") || serviceName.Contains("massage"))
            {
                service.AddOption(
                    "آروماتراپی - Aromatherapy",
                    Price.Create(500000m, "IRR"),
                    Duration.FromMinutes(0)); // No extra time
                optionsAdded++;

                service.AddOption(
                    "سنگ داغ - Hot Stone",
                    Price.Create(800000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;
            }

            // Facial/skin services options
            if (serviceName.Contains("پاکسازی") || serviceName.Contains("facial") || serviceName.Contains("پوست"))
            {
                service.AddOption(
                    "ماسک طلا - Gold Mask",
                    Price.Create(1200000m, "IRR"),
                    Duration.FromMinutes(20));
                optionsAdded++;

                service.AddOption(
                    "میکرودرم - Microdermabrasion",
                    Price.Create(1000000m, "IRR"),
                    Duration.FromMinutes(30));
                optionsAdded++;
            }

            // Manicure/pedicure options
            if (serviceName.Contains("مانیکور") || serviceName.Contains("manicure") ||
                serviceName.Contains("پدیکور") || serviceName.Contains("pedicure"))
            {
                service.AddOption(
                    "ژل - Gel Polish",
                    Price.Create(300000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;

                service.AddOption(
                    "طراحی ناخن - Nail Art",
                    Price.Create(500000m, "IRR"),
                    Duration.FromMinutes(20));
                optionsAdded++;

                service.AddOption(
                    "اکستنشن ناخن - Nail Extension",
                    Price.Create(800000m, "IRR"),
                    Duration.FromMinutes(45));
                optionsAdded++;
            }

            // Makeup services options
            if (serviceName.Contains("آرایش") || serviceName.Contains("makeup"))
            {
                service.AddOption(
                    "مژه مصنوعی - False Lashes",
                    Price.Create(400000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;

                service.AddOption(
                    "کانتور حرفه‌ای - Professional Contouring",
                    Price.Create(600000m, "IRR"),
                    Duration.FromMinutes(20));
                optionsAdded++;

                service.AddOption(
                    "تتو ابرو موقت - Temporary Eyebrow Tint",
                    Price.Create(300000m, "IRR"),
                    Duration.FromMinutes(10));
                optionsAdded++;
            }

            // Laser/clinic options
            if (serviceName.Contains("لیزر") || serviceName.Contains("laser"))
            {
                service.AddOption(
                    "ناحیه اضافی - Extra Area",
                    Price.Create(500000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;

                service.AddOption(
                    "کرم بیحس کننده - Numbing Cream",
                    Price.Create(200000m, "IRR"),
                    Duration.FromMinutes(5));
                optionsAdded++;
            }

            // Fitness/gym options
            if (serviceName.Contains("تمرین") || serviceName.Contains("training") ||
                serviceName.Contains("یوگا") || serviceName.Contains("yoga"))
            {
                service.AddOption(
                    "برنامه غذایی - Meal Plan",
                    Price.Create(500000m, "IRR"),
                    null);
                optionsAdded++;

                service.AddOption(
                    "ارزیابی بدن - Body Assessment",
                    Price.Create(300000m, "IRR"),
                    Duration.FromMinutes(15));
                optionsAdded++;
            }

            // Shaving/barbershop options
            if (serviceName.Contains("اصلاح") || serviceName.Contains("shave") || serviceName.Contains("ریش"))
            {
                service.AddOption(
                    "حوله داغ - Hot Towel",
                    Price.Create(100000m, "IRR"),
                    Duration.FromMinutes(5));
                optionsAdded++;

                service.AddOption(
                    "ماساژ صورت - Face Massage",
                    Price.Create(250000m, "IRR"),
                    Duration.FromMinutes(10));
                optionsAdded++;
            }

            return optionsAdded;
        }
    }
}
