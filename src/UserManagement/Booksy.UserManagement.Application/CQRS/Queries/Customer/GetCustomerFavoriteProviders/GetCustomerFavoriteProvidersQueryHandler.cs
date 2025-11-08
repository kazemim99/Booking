// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/GetCustomerFavoriteProvidersQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    public sealed class GetCustomerFavoriteProvidersQueryHandler : IQueryHandler<GetCustomerFavoriteProvidersQuery, List<FavoriteProviderViewModel>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<GetCustomerFavoriteProvidersQueryHandler> _logger;

        public GetCustomerFavoriteProvidersQueryHandler(
            ICustomerRepository customerRepository,
            ILogger<GetCustomerFavoriteProvidersQueryHandler> logger)
        {
            _customerRepository = customerRepository;
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

                return customer.FavoriteProviders
                    .Select(fp => new FavoriteProviderViewModel
                    {
                        ProviderId = fp.ProviderId,
                        Notes = fp.Notes,
                        AddedAt = fp.AddedAt
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
