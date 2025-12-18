// ========================================
// Booksy.ServiceCatalog.Application/Queries/Platform/GetPlatformStatistics/GetPlatformStatisticsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Platform.GetPlatformStatistics
{
    /// <summary>
    /// Handler for getting platform-wide statistics
    /// </summary>
    public sealed class GetPlatformStatisticsQueryHandler
        : IQueryHandler<GetPlatformStatisticsQuery, PlatformStatisticsViewModel>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetPlatformStatisticsQueryHandler> _logger;

        public GetPlatformStatisticsQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetPlatformStatisticsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<PlatformStatisticsViewModel> Handle(
            GetPlatformStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting platform-wide statistics");

            try
            {
                // Get all active providers
                var activeProviders = await _providerRepository.GetByStatusAsync(ProviderStatus.Active, cancellationToken);

                // Get unique cities
                var cities = activeProviders
                    .Where(p => !string.IsNullOrEmpty(p.Address?.City))
                    .Select(p => p.Address.City)
                    .Distinct()
                    .ToList();

                // Get all services from active providers
                var allServices = new List<Booksy.ServiceCatalog.Domain.Aggregates.Service>();
                foreach (var provider in activeProviders)
                {
                    var services = await _serviceRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
                    allServices.AddRange(services);
                }

                var activeServices = allServices
                    .Where(s => s.Status == ServiceStatus.Active)
                    .ToList();

                // Calculate category distribution
                var categoryDistribution = activeServices
                    .GroupBy(s => s.Category.Name)
                    .OrderByDescending(g => g.Count())
                    .Take(8) // Top 8 categories
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate average rating (mock data for now - would come from Reviews context)
                var avgRating = activeProviders.Any() ? 4.7m : 0m; // Mock average rating

                // Mock customer count (would come from Customer context)
                // For seed data, we can estimate based on bookings ratio
                var estimatedCustomers = activeProviders.Count * 50; // Rough estimate

                // Mock total bookings (would come from Booking context)
                var estimatedBookings = activeProviders.Count * 125; // Rough estimate

                var viewModel = new PlatformStatisticsViewModel
                {
                    TotalProviders = activeProviders.Count,
                    TotalCustomers = estimatedCustomers,
                    TotalBookings = estimatedBookings,
                    AverageRating = avgRating,
                    TotalServices = activeServices.Count,
                    CitiesWithProviders = cities.Count,
                    PopularCategories = categoryDistribution,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Platform statistics retrieved: {Providers} providers, {Services} services, {Cities} cities",
                    viewModel.TotalProviders, viewModel.TotalServices, viewModel.CitiesWithProviders);

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting platform statistics");
                throw;
            }
        }
    }
}
