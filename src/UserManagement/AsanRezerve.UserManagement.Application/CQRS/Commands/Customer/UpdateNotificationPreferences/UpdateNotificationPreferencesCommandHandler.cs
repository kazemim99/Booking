// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/Customer/UpdateNotificationPreferences/UpdateNotificationPreferencesCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates.CustomerAggregate;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences
{
    /// <summary>
    /// Handler for UpdateNotificationPreferencesCommand
    /// </summary>
    public sealed class UpdateNotificationPreferencesCommandHandler : ICommandHandler<UpdateNotificationPreferencesCommand, UpdateNotificationPreferencesResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateNotificationPreferencesCommandHandler> _logger;

        public UpdateNotificationPreferencesCommandHandler(
            ICustomerRepository customerRepository,
            IUserManagementUnitOfWork unitOfWork,
            ILogger<UpdateNotificationPreferencesCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UpdateNotificationPreferencesResult> Handle(
            UpdateNotificationPreferencesCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating notification preferences for CustomerId: {CustomerId}", request.CustomerId);

                // Get customer
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // Create new notification preferences
                var preferences = NotificationPreferences.Create(
                    request.SmsEnabled,
                    request.EmailEnabled,
                    request.ReminderTiming);

                // Update customer preferences
                customer.UpdateNotificationPreferences(preferences);

                // Save changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);
                // Commit the UserManagement unit of work explicitly. The pipeline's
                // TransactionBehavior commits the ServiceCatalog context (DI last-wins),
                // so without this the change was tracked and then silently discarded.
                await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

                _logger.LogInformation("Successfully updated notification preferences for CustomerId: {CustomerId}", request.CustomerId);

                return UpdateNotificationPreferencesResult.Success(
                    customer.Id.Value,
                    preferences.SmsEnabled,
                    preferences.EmailEnabled,
                    preferences.ReminderTiming,
                    customer.LastModifiedAt ?? DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
