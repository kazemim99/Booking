// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetRecentlyVisitedProviders/GetRecentlyVisitedProvidersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
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
        private readonly ILogger<GetRecentlyVisitedProvidersQueryHandler> _logger;

        public GetRecentlyVisitedProvidersQueryHandler(
            ICustomerRepository customerRepository,
            ILogger<GetRecentlyVisitedProvidersQueryHandler> logger)
        {
            _customerRepository = customerRepository;
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

                // Get recently visited providers using the aggregate method
                var recentlyVisited = customer.GetRecentlyVisitedProviders(request.Limit);

                var result = recentlyVisited
                    .Select(rv => new RecentlyVisitedProviderViewModel
                    {
                        ProviderId = rv.ProviderId,
                        VisitedAt = rv.VisitedAt,
                        ViewSource = rv.ViewSource
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
