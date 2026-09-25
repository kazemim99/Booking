// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Handler for RemoveFavoriteProviderCommand
    /// </summary>
    public sealed class RemoveFavoriteProviderCommandHandler : ICommandHandler<RemoveFavoriteProviderCommand, RemoveFavoriteProviderResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveFavoriteProviderCommandHandler> _logger;

        public RemoveFavoriteProviderCommandHandler(
            ICustomerRepository customerRepository,
            IUserManagementUnitOfWork unitOfWork,
            ILogger<RemoveFavoriteProviderCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
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
                // The aggregate ignores a provider that is not a favorite; the API contract is 404.
                if (!customer.FavoriteProviders.Any(fp => fp.ProviderId == request.ProviderId))
                {
                    throw new NotFoundException("FavoriteProvider", request.ProviderId);
                }

                customer.RemoveFavoriteProvider(request.ProviderId);

                // Persist changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);
                // Commit the UserManagement unit of work explicitly. The pipeline's
                // TransactionBehavior commits the ServiceCatalog context (DI last-wins),
                // so without this the change was tracked and then silently discarded.
                await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

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
