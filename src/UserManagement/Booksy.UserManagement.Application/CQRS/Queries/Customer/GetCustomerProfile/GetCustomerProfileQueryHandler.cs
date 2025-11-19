// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerProfile/GetCustomerProfileQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerProfile
{
    /// <summary>
    /// Handler for GetCustomerProfileQuery
    /// </summary>
    public sealed class GetCustomerProfileQueryHandler : IQueryHandler<GetCustomerProfileQuery, CustomerProfileViewModel>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetCustomerProfileQueryHandler> _logger;

        public GetCustomerProfileQueryHandler(
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ILogger<GetCustomerProfileQueryHandler> logger)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CustomerProfileViewModel> Handle(
            GetCustomerProfileQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting customer profile for CustomerId: {CustomerId}", request.CustomerId);

                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // Get associated user for email and phone
                var user = await _userRepository.GetByIdAsync(customer.UserId, cancellationToken);

                if (user == null)
                {
                    throw new InvalidOperationException($"User not found with ID: {customer.UserId}");
                }

                return new CustomerProfileViewModel
                {
                    CustomerId = customer.Id.Value,
                    UserId = customer.UserId.Value,
                    FirstName = user.Profile?.FirstName ?? string.Empty,
                    LastName = user.Profile?.LastName ?? string.Empty,
                    FullName = user.Profile?.GetFullName() ?? string.Empty,
                    Email = user.Email.Value,
                    PhoneNumber = user.PhoneNumber?.Value,
                    SmsEnabled = customer.NotificationPreferences.SmsEnabled,
                    EmailEnabled = customer.NotificationPreferences.EmailEnabled,
                    ReminderTiming = customer.NotificationPreferences.ReminderTiming,
                    IsActive = customer.IsActive,
                    CreatedAt = customer.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer profile for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
