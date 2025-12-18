// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateCustomerProfile/UpdateCustomerProfileCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile
{
    /// <summary>
    /// Handler for UpdateCustomerProfileCommand
    /// </summary>
    public sealed class UpdateCustomerProfileCommandHandler : ICommandHandler<UpdateCustomerProfileCommand, UpdateCustomerProfileResult>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateCustomerProfileCommandHandler> _logger;

        public UpdateCustomerProfileCommandHandler(
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ILogger<UpdateCustomerProfileCommandHandler> logger)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UpdateCustomerProfileResult> Handle(
            UpdateCustomerProfileCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating customer profile for CustomerId: {CustomerId}", request.CustomerId);

                // Get customer
                var customerId = CustomerId.From(request.CustomerId);
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    throw new InvalidOperationException($"Customer not found with ID: {request.CustomerId}");
                }

                // Get associated user (User aggregate owns the Profile)
                var user = await _userRepository.GetByIdAsync(customer.UserId, cancellationToken);

                if (user == null)
                {
                    throw new InvalidOperationException($"User not found with ID: {customer.UserId}");
                }

                // Update user profile with new information
                user.Profile.UpdateName(
                    request.FirstName,
                    request.LastName,
                    request.MiddleName);

                // Update contact information if provided
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var phoneNumber = PhoneNumber.From(request.PhoneNumber);
                    Address? address = null;

                    if (request.Address != null)
                    {
                        address = Address.Create(
                            request.Address.Street,
                            request.Address.City,
                            request.Address.State,
                            request.Address.PostalCode,
                            request.Address.Country,
                            request.Address.Unit);
                    }

                    user.Profile.UpdateContactInfo(phoneNumber, null, address);
                }

                // Update avatar if provided
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    user.Profile.UpdateAvatar(request.AvatarUrl);
                }

                // Update bio if provided
                if (!string.IsNullOrWhiteSpace(request.Bio))
                {
                    user.Profile.UpdateBio(request.Bio);
                }

                // Update date of birth and gender if provided
                if (request.DateOfBirth.HasValue)
                {
                    user.Profile.UpdateDateOfBirth(request.DateOfBirth.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Gender))
                {
                    user.Profile.UpdateGender(request.Gender);
                }

                // Persist changes to User (which owns the Profile)
                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogInformation(
                    "Customer profile updated successfully. CustomerId: {CustomerId}, UserId: {UserId}",
                    customer.Id, user.Id);

                return new UpdateCustomerProfileResult(
                    CustomerId: customer.Id.Value,
                    FullName: user.Profile.GetFullName(),
                    UpdatedAt: DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update customer profile for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
