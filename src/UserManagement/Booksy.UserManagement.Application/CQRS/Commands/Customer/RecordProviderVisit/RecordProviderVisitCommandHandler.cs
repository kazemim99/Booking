// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RecordProviderVisit/RecordProviderVisitCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RecordProviderVisit
{
    /// <summary>
    /// Handler for RecordProviderVisitCommand
    /// </summary>
    public sealed class RecordProviderVisitCommandHandler : ICommandHandler<RecordProviderVisitCommand, RecordProviderVisitResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<RecordProviderVisitCommandHandler> _logger;

        public RecordProviderVisitCommandHandler(
            ICustomerRepository customerRepository,
            ILogger<RecordProviderVisitCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<RecordProviderVisitResult> Handle(
            RecordProviderVisitCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Recording provider visit. CustomerId: {CustomerId}, ProviderId: {ProviderId}, ViewSource: {ViewSource}",
                    request.CustomerId,
                    request.ProviderId,
                    request.ViewSource ?? "Unknown");

                // Get customer
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // Record visit
                var visitedAt = DateTime.UtcNow;
                customer.RecordProviderVisit(request.ProviderId, request.ViewSource);

                // Persist changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                _logger.LogInformation(
                    "Provider visit recorded successfully. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    customer.Id,
                    request.ProviderId);

                return new RecordProviderVisitResult(
                    CustomerId: customer.Id.Value,
                    ProviderId: request.ProviderId,
                    VisitedAt: visitedAt,
                    ViewSource: request.ViewSource,
                    TotalRecentlyVisited: customer.RecentlyVisited.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to record provider visit. CustomerId: {CustomerId}, ProviderId: {ProviderId}",
                    request.CustomerId,
                    request.ProviderId);
                throw;
            }
        }
    }
}
