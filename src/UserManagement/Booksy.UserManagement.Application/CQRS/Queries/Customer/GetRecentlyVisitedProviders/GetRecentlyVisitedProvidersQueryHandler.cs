// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/GetRecentlyVisitedProvidersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetRecentlyVisitedProviders
{
    /// <summary>
    /// Handler for GetRecentlyVisitedProvidersQuery
    /// </summary>
    public sealed class GetRecentlyVisitedProvidersQueryHandler : IQueryHandler<GetRecentlyVisitedProvidersQuery, List<RecentlyVisitedProviderViewModel>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IProviderReadRepository _providers;
        private readonly IUrlService _urls;
        private readonly ILogger<GetRecentlyVisitedProvidersQueryHandler> _logger;

        public GetRecentlyVisitedProvidersQueryHandler(
            ICustomerRepository customerRepository,
            IProviderReadRepository providers,
            IUrlService urls,
            ILogger<GetRecentlyVisitedProvidersQueryHandler> logger)
        {
            _customerRepository = customerRepository;
            _providers = providers;
            _urls = urls;
            _logger = logger;
        }

        public async Task<List<RecentlyVisitedProviderViewModel>> Handle(
            GetRecentlyVisitedProvidersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Getting recently visited providers for customer: {CustomerId}, Limit: {Limit}",
                    request.CustomerId,
                    request.Limit);

                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // The whole stored history (the aggregate caps it), newest first, one row per salon.
                // The limit is applied only after hidden salons are dropped, so a gone or inactive
                // salon never costs the customer a row they could have seen.
                var visits = customer.GetRecentlyVisitedProviders(int.MaxValue)
                    .GroupBy(rv => rv.ProviderId)
                    .Select(g => new
                    {
                        Newest = g.OrderByDescending(rv => rv.VisitedAt).First(),
                        Count = g.Count()
                    })
                    .OrderByDescending(v => v.Newest.VisitedAt)
                    .ToList();

                var salons = await SalonSummaryLookup.FindActiveAsync(
                    _providers,
                    _urls,
                    visits.Select(v => v.Newest.ProviderId),
                    cancellationToken);

                var result = visits
                    .Where(v => salons.ContainsKey(v.Newest.ProviderId))
                    .Take(request.Limit)
                    .Select(v =>
                    {
                        var salon = salons[v.Newest.ProviderId];
                        return new RecentlyVisitedProviderViewModel
                        {
                            ProviderId = v.Newest.ProviderId,
                            VisitedAt = v.Newest.VisitedAt,
                            ViewSource = v.Newest.ViewSource,
                            ProviderName = salon.Name,
                            LogoUrl = salon.LogoUrl,
                            City = salon.City,
                            AverageRating = salon.AverageRating,
                            TotalReviews = salon.TotalReviews,
                            LastVisitedAt = v.Newest.VisitedAt,
                            VisitCount = v.Count
                        };
                    })
                    .ToList();

                _logger.LogInformation(
                    "Retrieved {Count} recently visited providers for customer: {CustomerId}",
                    result.Count,
                    request.CustomerId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get recently visited providers for customer: {CustomerId}",
                    request.CustomerId);
                throw;
            }
        }
    }
}
