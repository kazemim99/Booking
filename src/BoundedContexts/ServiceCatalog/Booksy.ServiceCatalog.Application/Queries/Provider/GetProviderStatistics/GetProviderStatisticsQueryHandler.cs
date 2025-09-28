﻿// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderStatistics/GetProviderStatisticsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStatistics
{
    public sealed class GetProviderStatisticsQueryHandler : IQueryHandler<GetProviderStatisticsQuery, ProviderStatisticsViewModel>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetProviderStatisticsQueryHandler> _logger;

        public GetProviderStatisticsQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetProviderStatisticsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<ProviderStatisticsViewModel> Handle(
            GetProviderStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting statistics for provider: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.Create(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
            {
                throw new InvalidOperationException("Provider not found");
            }

            var services = await _serviceRepository.GetByProviderIdAsync(providerId, cancellationToken);

            // Calculate statistics (in real implementation, this would come from various contexts)
            var activeServices = services.Count(s => s.Status == Domain.Enums.ServiceStatus.Active);
            var totalStaff = provider.Staff.Count(s => s.IsActive);

            // Mock data for statistics that would come from other contexts
            var totalBookings = 0; // From Booking context
            var totalRevenue = 0m; // From Payment context  
            var averageRating = 0.0m; // From Reviews context

            var viewModel = new ProviderStatisticsViewModel
            {
                ProviderId = provider.Id.Value,
                BusinessName = provider.Profile.BusinessName,
                TotalServices = services.Count,
                ActiveServices = activeServices,
                TotalStaff = totalStaff,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                Currency = "USD", // Default currency
                AverageRating = averageRating,
                RegisteredAt = provider.RegisteredAt,
                LastActiveAt = provider.LastActiveAt,
                ServicesByCategory = services.GroupBy(s => s.Category.Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                BookingsByMonth = new Dictionary<string, int>(), // Would be populated from Booking context
                RevenueByMonth = new Dictionary<string, decimal>(), // Would be populated from Payment context
                ActiveBookingsThisMonth = 0,
                RevenueThisMonth = 0m,
                GrowthRate = 0.0
            };

            return viewModel;
        }
    }
}