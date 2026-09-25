// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/GetRecentlyVisitedProvidersQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetRecentlyVisitedProviders
{
    /// <summary>
    /// Handler for GetRecentlyVisitedProvidersQuery
    /// </summary>
    public sealed class GetRecentlyVisitedProvidersQueryHandler : IQueryHandler<GetRecentlyVisitedProvidersQuery, List<RecentlyVisitedProviderViewModel>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IProviderInfoService _salons;
        private readonly ILogger<GetRecentlyVisitedProvidersQueryHandler> _logger;

        public GetRecentlyVisitedProvidersQueryHandler(
            ICustomerRepository customerRepository,
            IProviderInfoService salons,
            ILogger<GetRecentlyVisitedProvidersQueryHandler> logger)
        {
            _customerRepository = customerRepository;
            _salons = salons;
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

                var salons = await _salons.GetActiveSalonCardsAsync(
                    visits.Select(v => v.Newest.ProviderId).ToList(),
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
