// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/SearchProviders/SearchProvidersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    public sealed class SearchProvidersQueryHandler : IQueryHandler<SearchProvidersQuery, IReadOnlyList<ProviderSearchResultViewModel>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<SearchProvidersQueryHandler> _logger;

        public SearchProvidersQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<SearchProvidersQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProviderSearchResultViewModel>> Handle(
            SearchProvidersQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Searching providers with term: {SearchTerm}", request.SearchTerm);

            var allProviders = await _providerRepository.SearchAsync(request.SearchTerm, cancellationToken);

            var filteredProviders = allProviders.Where(provider =>
                (request.ProviderType == null || provider.Type == request.ProviderType) &&
                (request.City == null || provider.Address.City.Contains(request.City, StringComparison.OrdinalIgnoreCase)) &&
                (request.State == null || provider.Address.State.Contains(request.State, StringComparison.OrdinalIgnoreCase)) &&
                (request.OffersMobileServices == null || provider.OffersMobileServices == request.OffersMobileServices));

            var result = filteredProviders
                .Take(request.MaxResults ?? 50)
                .Select(provider => new ProviderSearchResultViewModel
                {
                    Id = provider.Id.Value,
                    BusinessName = provider.Profile.BusinessName,
                    Description = provider.Profile.Description,
                    Type = provider.Type,
                    LogoUrl = provider.Profile.LogoUrl,
                    Address = $"{provider.Address.City}, {provider.Address.State}",
                    Email = provider.ContactInfo.Email.Value,
                    PrimaryPhone = provider.ContactInfo.PrimaryPhone.Value,
                    Website = provider.ContactInfo.Website,
                    AllowOnlineBooking = provider.AllowOnlineBooking,
                    OffersMobileServices = provider.OffersMobileServices,
                    Tags = provider.Profile.Tags,
                    LastActiveAt = provider.LastActiveAt
                })
                .ToList();

            return result;
        }
    }
}