// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/AddFavoriteProvider/AddFavoriteProviderCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.UserManagement.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider
{
    /// <summary>
    /// Handler for AddFavoriteProviderCommand
    /// </summary>
    public sealed class AddFavoriteProviderCommandHandler : ICommandHandler<AddFavoriteProviderCommand, AddFavoriteProviderResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly ILogger<AddFavoriteProviderCommandHandler> _logger;

        public AddFavoriteProviderCommandHandler(
            ICustomerRepository customerRepository,
            IUserManagementUnitOfWork unitOfWork,
            ILogger<AddFavoriteProviderCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AddFavoriteProviderResult> Handle(
            AddFavoriteProviderCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Adding favorite provider. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    request.CustomerId,
                    request.ProviderId);

                // Get customer
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // The aggregate treats a duplicate as a silent no-op; the API contract is 409.
                if (customer.FavoriteProviders.Any(fp => fp.ProviderId == request.ProviderId))
                {
                    throw new ConflictException($"Provider {request.ProviderId} is already a favorite of customer {request.CustomerId}");
                }

                // Add favorite provider
                var addedAt = DateTime.UtcNow;
                customer.AddFavoriteProvider(request.ProviderId, request.Notes);

                // Persist changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);
                // Commit the UserManagement unit of work explicitly. The pipeline's
                // TransactionBehavior commits the ServiceCatalog context (DI last-wins),
                // so without this the change was tracked and then silently discarded.
                await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

                _logger.LogInformation(
                    "Favorite provider added successfully. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    customer.Id,
                    request.ProviderId);

                return new AddFavoriteProviderResult(
                    CustomerId: customer.Id.Value,
                    ProviderId: request.ProviderId,
                    AddedAt: addedAt,
                    TotalFavorites: customer.FavoriteProviders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to add favorite provider. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    request.CustomerId,
                    request.ProviderId);
                throw;
            }
        }
    }
}
