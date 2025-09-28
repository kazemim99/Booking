////===========================================
//// Provider Query Handlers and Specifications
////===========================================

////===========================================
//// Queries/Provider/SearchProviders/SearchProvidersQueryHandler.cs
////===========================================
//using Booksy.Core.Application.Abstractions.CQRS;
//using Booksy.ServiceCatalog.Application.DTOs.Provider;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Booksy.ServiceCatalog.Domain.Specifications.Provider;
//using Microsoft.Extensions.Logging;

//namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetFeaturedProviders
//{
//    public sealed class GetFeaturedProvidersQueryHandler : IQueryHandler<GetFeaturedProvidersQuery, IReadOnlyList<ProviderSummaryItem>>
//    {
//        private readonly IProviderReadRepository _providerRepository;
//        private readonly ILogger<GetFeaturedProvidersQueryHandler> _logger;

//        public GetFeaturedProvidersQueryHandler(
//            IProviderReadRepository providerRepository,
//            ILogger<GetFeaturedProvidersQueryHandler> logger)
//        {
//            _providerRepository = providerRepository;
//            _logger = logger;
//        }

//        public async Task<IReadOnlyList<ProviderSummaryItem>> Handle(
//            GetFeaturedProvidersQuery request,
//            CancellationToken cancellationToken)
//        {
//            _logger.LogDebug("Getting featured providers, Category: {Category}, Limit: {Limit}",
//                request.CategoryFilter, request.Limit);

//            try
//            {
//                var specification = new FeaturedProvidersSpecification(request.CategoryFilter);

//                var providers = await _providerRepository.GetAsync(specification, cancellationToken);

//                var result = providers
//                    .Take(request.Limit)
//                    .Select(provider => new ProviderSummaryItem(
//                        Id: provider.Id.Value,
//                        BusinessName: provider.Profile.BusinessName,
//                        Description: provider.Profile.Description,
//                        Type: provider.Type,
//                        Status: provider.Status,
//                        City: provider.Address.City,
//                        State: provider.Address.State,
//                        Country: provider.Address.Country,
//                        LogoUrl: provider.Profile.LogoUrl,
//                        AllowOnlineBooking: provider.AllowOnlineBooking,
//                        OffersMobileServices: provider.OffersMobileServices,
//                        IsVerified: provider.IsVerified,
//                        AverageRating: provider.AverageRating,
//                        TotalReviews: provider.TotalReviews,
//                        ServiceCount: provider.Services.Count,
//                        RegisteredAt: provider.RegisteredAt))
//                    .ToList();

//                _logger.LogInformation("Retrieved {Count} featured providers", result.);
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting featured providers");
//                throw;
//            }
//        }
//    }
//}

