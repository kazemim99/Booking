using Booksy.Core.Domain.Domain.Entities;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeder for populating the ProvinceCities table with Iranian provinces and cities
    /// </summary>
    public sealed class ProvinceCitiesSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ProvinceCitiesSeeder> _logger;
        private readonly string _jsonFilePath;

        public ProvinceCitiesSeeder(
            ServiceCatalogDbContext context,
            ILogger<ProvinceCitiesSeeder> logger)
        {
            _context = context;
            _logger = logger;
            _jsonFilePath = "ProvinceCity-ParentChild.json";
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
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

                _logger.LogInformation("Starting Iranian location seeding from {FilePath}", _jsonFilePath);

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

                _logger.LogInformation("Successfully seeded {Count} Iranian locations ({ProvinceCount} provinces, {CityCount} cities)",
                    locations.Count,
                    locations.Count(l => l.Type == "Province"),
                    locations.Count(l => l.Type == "City"));
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Iranian location JSON file not found at {FilePath}", _jsonFilePath);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing Iranian location JSON file");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian locations");
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
    }
}
