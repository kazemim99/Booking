using Booksy.Core.Domain.Domain.Entities;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    public sealed class ServiceCatalogDatabaseSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceCatalogDatabaseSeeder> _logger;
        private readonly string _jsonFilePath;

        public ServiceCatalogDatabaseSeeder(
            ServiceCatalogDbContext context,
            ILogger<ServiceCatalogDatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
            _jsonFilePath = "ProvinceCity-ParentChild.json";
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {

                await SeedProvidersAsync(cancellationToken);

                // Seed staff for providers
                var staffSeeder = new StaffDataSeeder(
                    _context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<StaffDataSeeder>.Instance);
                await staffSeeder.SeedAsync(cancellationToken);

                await SeedServicesAsync(cancellationToken);
                await SeedProviceCitiesAsync(cancellationToken);

                // Seed notification templates
                var templateSeeder = new NotificationTemplateSeeder(
                    _context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationTemplateSeeder>.Instance);
                await templateSeeder.SeedAsync(cancellationToken);

                // Save changes before seeding bookings (bookings depend on providers/services/staff)
                await _context.SaveChangesAsync(cancellationToken);

                // Seed booking data
                var bookingSeeder = new BookingDataSeeder(
                    _context,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingDataSeeder>.Instance);
                await bookingSeeder.SeedAsync(cancellationToken);

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


            if (await _context.Providers.AnyAsync(cancellationToken))
            {
                return;
            }

            var providers = new[]
            {
                CreateSampleProvider(
                    "Bella's Beauty Salon",
                    "Full-service beauty salon offering hair, nails, and skincare",
                    ProviderType.Individual,
                    "New York",
                    "bella@beautysalon.com"),

                CreateSampleProvider(
                    "Mike's Barbershop",
                    "Traditional barbershop with modern techniques",
                    ProviderType.Salon,
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

        public async Task SeedProviceCitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if locations already exist
                var locationsExist = await _context.Set<ProvinceCities>().AnyAsync(cancellationToken);
                if (locationsExist)
                {
                    _logger.LogInformation("ProvinceCities already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting location seeding from {FilePath}", _jsonFilePath);

                // Read and parse the JSON file
                var jsonContent = await File.ReadAllTextAsync(_jsonFilePath, cancellationToken);
                var locationData = JsonSerializer.Deserialize<List<LocationDto>>(jsonContent);

                if (locationData == null || !locationData.Any())
                {
                    _logger.LogWarning("No location data found in {FilePath}", _jsonFilePath);
                    return;
                }

                // Convert DTOs to entities
                var locations = new List<ProvinceCities>();
                foreach (var dto in locationData)
                {
                    var location = dto.Type == "Province"
                        ? ProvinceCities.CreateProvince(dto.Id, dto.Name, dto.ProvinceCode)
                        : ProvinceCities.CreateCity(dto.Id, dto.Name, dto.ProvinceCode, dto.CityCode!.Value, dto.ParentId!.Value);

                    locations.Add(location);
                }

                // Add to database
                await _context.Set<ProvinceCities>().AddRangeAsync(locations, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} locations ({ProvinceCount} provinces, {CityCount} cities)",
                    locations.Count,
                    locations.Count(l => l.Type == "Province"),
                    locations.Count(l => l.Type == "City"));
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Location JSON file not found at {FilePath}", _jsonFilePath);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing location JSON file");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding locations");
                throw;
            }
        }

        /// <summary>
        /// DTO for deserializing the JSON file
        /// </summary>
        private sealed class LocationDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int ProvinceCode { get; set; }
            public int? CityCode { get; set; }
            public int? ParentId { get; set; }
            public string Type { get; set; } = string.Empty;
        }

        private Provider CreateSampleProvider(
            string businessName,
            string description,
            ProviderType type,
            string city,
            string email)
        {
            var ownerId = UserId.From(Guid.NewGuid());
            var emailValue = Email.Create(email);
            var phoneValue = PhoneNumber.Create("5551234567");

            var contactInfo = ContactInfo.Create(
                emailValue,
                phoneValue,
                null,
                $"https://www.{businessName.Replace(" ", "").Replace("'", "").ToLower()}.com");

            var address = BusinessAddress.Create(
                $"123 Main Street, {city}, State",
                "123 Main Street",
                city,
                "State",
                "12345",
                "United States",
                null,
                null,
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
            try
            {

           
            if (await _context.Services.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("ServiceCatalog database already seeded");
                return;
            }


            var providers = await _context.Providers.ToListAsync(cancellationToken);
            var services = new List<Service>();

            foreach (var provider in providers)
            {
                services.AddRange(CreateSampleServicesForProvider(provider));
            }

            await _context.Services.AddRangeAsync(services, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        private List<Service> CreateSampleServicesForProvider(Provider provider)
        {
            var services = new List<Service>();
            var baseServices = GetBaseServicesByProviderType(provider.ProviderType);

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
