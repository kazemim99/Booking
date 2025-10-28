using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus
{
    public sealed class GetProvidersByStatusQueryHandler : IQueryHandler<GetProvidersByStatusQuery, IReadOnlyList<ProviderListViewModel>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<GetProvidersByStatusQueryHandler> _logger;

        public GetProvidersByStatusQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<GetProvidersByStatusQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProviderListViewModel>> Handle(
            GetProvidersByStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting providers by status: {Status}", request.Status);

            var providers = await _providerRepository.GetByStatusAsync(request.Status, cancellationToken);

            var result = providers
                .Take(request.MaxResults ?? int.MaxValue)
                .Select(provider => new ProviderListViewModel
                {
                    Id = provider.Id.Value,
                    BusinessName = provider.Profile.BusinessName,
                    Description = provider.Profile.BusinessDescription,
                    Status = provider.Status,
                    Type = provider.ProviderType,
                    City = provider.Address.City,
                    State = provider.Address.State,
                    Country = provider.Address.Country,
                    Email = provider.ContactInfo.Email?.Value ?? string.Empty,
                    PrimaryPhone = provider.ContactInfo.PrimaryPhone?.Value ?? string.Empty,
                    LogoUrl = provider.Profile.LogoUrl,
                    AllowOnlineBooking = provider.AllowOnlineBooking,
                    OffersMobileServices = provider.OffersMobileServices,
                    IsVerified = provider.VerifiedAt.HasValue,
                    AverageRating = provider.AverageRating,
                    TotalReviews = 0, // TODO: Add review count when reviews are implemented
                    ServiceCount = provider.Services.Count,
                    RegisteredAt = provider.RegisteredAt,
                    LastActiveAt = provider.LastActiveAt
                })
                .ToList();

            return result;
        }
    }
}