// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/GetCustomerFavoriteProvidersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    public sealed class GetCustomerFavoriteProvidersQueryHandler : IQueryHandler<GetCustomerFavoriteProvidersQuery, List<FavoriteProviderViewModel>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IProviderReadRepository _providers;
        private readonly IUrlService _urls;
        private readonly ILogger<GetCustomerFavoriteProvidersQueryHandler> _logger;

        public GetCustomerFavoriteProvidersQueryHandler(
            ICustomerRepository customerRepository,
            IProviderReadRepository providers,
            IUrlService urls,
            ILogger<GetCustomerFavoriteProvidersQueryHandler> logger)
        {
            _customerRepository = customerRepository;
            _providers = providers;
            _urls = urls;
            _logger = logger;
        }

        public async Task<List<FavoriteProviderViewModel>> Handle(
            GetCustomerFavoriteProvidersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                var salons = await SalonSummaryLookup.FindActiveAsync(
                    _providers,
                    _urls,
                    customer.FavoriteProviders.Select(fp => fp.ProviderId),
                    cancellationToken);

                // A favourite whose salon is gone or no longer Active is left out: the app cannot
                // open it, and a row without a name used to fail the whole Home section.
                return customer.FavoriteProviders
                    .Where(fp => salons.ContainsKey(fp.ProviderId))
                    .Select(fp =>
                    {
                        var salon = salons[fp.ProviderId];
                        return new FavoriteProviderViewModel
                        {
                            ProviderId = fp.ProviderId,
                            Notes = fp.Notes,
                            AddedAt = fp.AddedAt,
                            ProviderName = salon.Name,
                            LogoUrl = salon.LogoUrl,
                            City = salon.City,
                            AverageRating = salon.AverageRating,
                            TotalReviews = salon.TotalReviews
                        };
                    })
                    .OrderByDescending(fp => fp.AddedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer favorite providers: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
