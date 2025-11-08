// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Handler for RemoveFavoriteProviderCommand
    /// </summary>
    public sealed class RemoveFavoriteProviderCommandHandler : ICommandHandler<RemoveFavoriteProviderCommand, RemoveFavoriteProviderResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<RemoveFavoriteProviderCommandHandler> _logger;

        public RemoveFavoriteProviderCommandHandler(
            ICustomerRepository customerRepository,
            ILogger<RemoveFavoriteProviderCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<RemoveFavoriteProviderResult> Handle(
            RemoveFavoriteProviderCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Removing favorite provider. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    request.CustomerId,
                    request.ProviderId);

                // Get customer
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // Remove favorite provider
                var removedAt = DateTime.UtcNow;
                customer.RemoveFavoriteProvider(request.ProviderId);

                // Persist changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                _logger.LogInformation(
                    "Favorite provider removed successfully. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    customer.Id,
                    request.ProviderId);

                return new RemoveFavoriteProviderResult(
                    CustomerId: customer.Id.Value,
                    ProviderId: request.ProviderId,
                    RemovedAt: removedAt,
                    RemainingFavorites: customer.FavoriteProviders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to remove favorite provider. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    request.CustomerId,
                    request.ProviderId);
                throw;
            }
        }
    }
}
