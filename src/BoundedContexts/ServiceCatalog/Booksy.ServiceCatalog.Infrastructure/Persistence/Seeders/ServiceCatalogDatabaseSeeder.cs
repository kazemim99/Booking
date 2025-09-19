using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    public sealed class ServiceCatalogDatabaseSeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceCatalogDatabaseSeeder> _logger;

        public ServiceCatalogDatabaseSeeder(
            ServiceCatalogDbContext context,
            ILogger<ServiceCatalogDatabaseSeeder> logger)
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
                    _logger.LogInformation("ServiceCatalog database already seeded");
                    return;
                }

                await SeedProvidersAsync(cancellationToken);
                await SeedServicesAsync(cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("ServiceCatalog database seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding ServiceCatalog database");
                throw;
            }
        }

        private async Task SeedProvidersAsync(CancellationToken cancellationToken)
        {
            var providers = new[]
            {
                CreateSampleProvider(
                    "Bella's Beauty Salon",
                    "Full-service beauty salon offering hair, nails, and skincare",
                    ProviderType.Salon,
                    "New York",
                    "bella@beautysalon.com"),

                CreateSampleProvider(
                    "Mike's Barbershop",
                    "Traditional barbershop with modern techniques",
                    ProviderType.Individual,
                    "Los Angeles",
                    "mike@barbershop.com"),

                CreateSampleProvider(
                    "Wellness Spa & Massage",
                    "Luxury spa offering massage therapy and wellness treatments",
                    ProviderType.Spa,
                    "Miami",
                    "info@wellnessspa.com")
            };

            await _context.Providers.AddRangeAsync(providers, cancellationToken);
        }

        private Provider CreateSampleProvider(
            string businessName,
            string description,
            ProviderType type,
            string city,
            string email)
        {
            var ownerId =  UserId.From(Guid.NewGuid());
            var emailValue = Email.From(email);
            var phoneValue = PhoneNumber.From("5551234567");

            var contactInfo = ContactInfo.Create(
                emailValue,
                phoneValue,
                null,
                $"https://www.{businessName.Replace(" ", "").Replace("'", "").ToLower()}.com");

            var address = BusinessAddress.Create(
                "123 Main Street",
                city,
                "State",
                "12345",
                "United States",
                40.7128,
                -74.0060);

            return Provider.RegisterProvider(
                ownerId,
                businessName,
                description,
                type,
                contactInfo,
                address);
        }

        private async Task SeedServicesAsync(CancellationToken cancellationToken)
        {
            var providers = await _context.Providers.ToListAsync(cancellationToken);
            var services = new List<Service>();

            foreach (var provider in providers)
            {
                services.AddRange(CreateSampleServicesForProvider(provider));
            }

            await _context.Services.AddRangeAsync(services, cancellationToken);
        }

        private List<Service> CreateSampleServicesForProvider(Provider provider)
        {
            var services = new List<Service>();
            var baseServices = GetBaseServicesByProviderType(provider.Type);

            foreach (var (name, description, price, duration) in baseServices)
            {
                var category = ServiceCategory.Create("Beauty", "Beauty and wellness services");
                var priceValue = Price.Create(price, "USD");
                var durationValue = Duration.FromMinutes(duration);

                var service = Service.Create(
                    provider.Id,
                    name,
                    description,
                    category,
                    ServiceType.Standard,
                    priceValue,
                    durationValue);

                services.Add(service);
            }

            return services;
        }

        private List<(string Name, string Description, decimal Price, int Duration)> GetBaseServicesByProviderType(ProviderType type)
        {
            return type switch
            {
                ProviderType.Individual => new List<(string, string, decimal, int)>
                {
                    ("Men's Haircut", "Professional men's haircut and styling", 35m, 30),
                    ("Beard Trim", "Precision beard trimming and shaping", 20m, 15),
                    ("Hot Towel Shave", "Traditional hot towel shave experience", 45m, 45)
                },
                ProviderType.Salon => new List<(string, string, decimal, int)>
                {
                    ("Women's Cut & Style", "Haircut with professional styling", 65m, 60),
                    ("Hair Color", "Professional hair coloring service", 120m, 120),
                    ("Manicure", "Classic manicure with nail care", 30m, 45),
                    ("Pedicure", "Relaxing pedicure treatment", 40m, 60)
                },
                ProviderType.Spa => new List<(string, string, decimal, int)>
                {
                    ("Swedish Massage", "Full body relaxation massage", 90m, 60),
                    ("Deep Tissue Massage", "Therapeutic deep tissue treatment", 110m, 60),
                    ("Facial Treatment", "Customized facial for all skin types", 80m, 75),
                    ("Body Wrap", "Detoxifying body wrap treatment", 150m, 90)
                },
                _ => new List<(string, string, decimal, int)>()
            };
        }
    }
}
