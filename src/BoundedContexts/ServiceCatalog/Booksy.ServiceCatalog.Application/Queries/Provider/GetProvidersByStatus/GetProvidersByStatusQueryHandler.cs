using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus
{
    public sealed class GetProvidersByStatusQueryHandler : IQueryHandler<GetProvidersByStatusQuery, IReadOnlyList<ProviderListViewModel>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetProvidersByStatusQueryHandler> _logger;

        public GetProvidersByStatusQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetProvidersByStatusQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProviderListViewModel>> Handle(
            GetProvidersByStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting providers by status: {Status}", request.Status);

            var providers = await _providerRepository.GetByStatusAsync(request.Status, cancellationToken);

            var page = providers.Take(request.MaxResults ?? int.MaxValue).ToList();

            // Services are their own aggregate: count them in the services table. provider.Services
            // is a stale collection that is always empty, which showed every provider in the admin
            // panel with zero services (2026-09-19). One count per row: this list is the admin's,
            // tens of providers at most.
            var serviceCounts = new Dictionary<Guid, int>();
            foreach (var provider in page)
            {
                serviceCounts[provider.Id.Value] =
                    await _serviceRepository.CountByProviderAsync(provider.Id, cancellationToken: cancellationToken);
            }

            var result = page
                .Select(provider => new ProviderListViewModel
                {
                    Id = provider.Id.Value,
                    BusinessName = provider.Profile.BusinessName,
                    Description = provider.Profile.BusinessDescription,
                    Status = provider.Status,
                    PrimaryCategory = provider.PrimaryCategory,
                    City = provider.Address.City,
                    State = provider.Address.State,
                    Country = provider.Address.Country,
                    Email = provider.ContactInfo.Email?.Value ?? string.Empty,
                    PrimaryPhone = provider.ContactInfo.PrimaryPhone?.Value ?? string.Empty,
                    LogoUrl = provider.Profile.DisplayImageUrl,
                    ProfileImageUrl = provider.Profile.ProfileImageUrl,
                    AllowOnlineBooking = provider.AllowOnlineBooking,
                    OffersMobileServices = provider.OffersMobileServices,
                    IsVerified = provider.VerifiedAt.HasValue,
                    AverageRating = provider.AverageRating,
                    TotalReviews = 0, // TODO: Add review count when reviews are implemented
                    ServiceCount = serviceCounts[provider.Id.Value],
                    RegisteredAt = provider.RegisteredAt,
                    LastActiveAt = provider.LastActiveAt
                })
                .ToList();

            return result;
        }
    }
}