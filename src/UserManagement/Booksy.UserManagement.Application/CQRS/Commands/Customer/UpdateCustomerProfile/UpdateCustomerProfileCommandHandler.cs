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
        private readonly ILogger<UpdateCustomerProfileCommandHandler> _logger;

        public UpdateCustomerProfileCommandHandler(
            ICustomerRepository customerRepository,
            ILogger<UpdateCustomerProfileCommandHandler> logger)
        {
            _customerRepository = customerRepository;
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

                // Create updated profile
                var updatedProfile = UserProfile.Create(
                    request.FirstName,
                    request.LastName,
                    request.MiddleName,
                    request.DateOfBirth,
                    request.Gender);

                // Update contact information
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
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

                    updatedProfile.UpdateContactInfo(phoneNumber, null, address);
                }

                // Update avatar
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                {
                    updatedProfile.UpdateAvatar(request.AvatarUrl);
                }

                // Update bio
                if (!string.IsNullOrWhiteSpace(request.Bio))
                {
                    updatedProfile.UpdateBio(request.Bio);
                }

                // Update customer profile
                customer.UpdateProfile(updatedProfile);

                // Persist changes
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                _logger.LogInformation(
                    "Customer profile updated successfully. CustomerId: {CustomerId}",
                    customer.Id);

                return new UpdateCustomerProfileResult(
                    CustomerId: customer.Id.Value,
                    FullName: updatedProfile.GetFullName(),
                    UpdatedAt: customer.LastModifiedAt ?? DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update customer profile for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
