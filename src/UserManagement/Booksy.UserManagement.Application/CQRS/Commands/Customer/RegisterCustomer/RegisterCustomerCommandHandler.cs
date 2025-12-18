// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RegisterCustomer/RegisterCustomerCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Exceptions;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RegisterCustomer
{
    /// <summary>
    /// Handler for RegisterCustomerCommand
    /// Creates both User and Customer aggregates
    /// </summary>
    public sealed class RegisterCustomerCommandHandler : ICommandHandler<RegisterCustomerCommand, RegisterCustomerResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserValidationService _validationService;
        private readonly ILogger<RegisterCustomerCommandHandler> _logger;

        public RegisterCustomerCommandHandler(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IPasswordHasher passwordHasher,
            IUserValidationService validationService,
            ILogger<RegisterCustomerCommandHandler> logger)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _passwordHasher = passwordHasher;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<RegisterCustomerResult> Handle(
            RegisterCustomerCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing customer registration for email: {Email}", request.Email);

                // Validate email availability
                var email = Email.Create(request.Email);
                var emailExists = await _validationService.IsEmailAvailableAsync(email, cancellationToken);

                if (emailExists)
                {
                    throw new UserAlreadyExistsException(email);
                }

                // Hash password
                var hashedPassword = _passwordHasher.HashPassword(request.Password);
                var password = HashedPassword.Create(hashedPassword);

                // Create user profile
                var profile = UserProfile.Create(
                    request.FirstName,
                    request.LastName,
                    request.MiddleName,
                    request.DateOfBirth,
                    request.Gender);

                // Set contact information
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

                profile.UpdateContactInfo(phoneNumber, null, address);

                // Set additional profile information
                if (!string.IsNullOrWhiteSpace(request.Bio))
                {
                    profile.UpdateBio(request.Bio);
                }

                // Create User aggregate
                var user = User.Register(email, password, profile, UserType.Customer);

                // Persist user first to ensure we have a valid UserId
                await _userRepository.SaveAsync(user, cancellationToken);

                // Create Customer aggregate
                var customer = Domain.Aggregates.CustomerAggregate.Customer.Create(user.Id, profile);

                // Persist customer
                await _customerRepository.SaveAsync(customer, cancellationToken);

                _logger.LogInformation(
                    "Customer registered successfully. CustomerId: {CustomerId}, UserId: {UserId}, Email: {Email}",
                    customer.Id,
                    user.Id,
                    user.Email);

                return new RegisterCustomerResult(
                    CustomerId: customer.Id.Value,
                    UserId: user.Id.Value,
                    Email: user.Email.Value,
                    FullName: profile.GetFullName(),
                    PhoneNumber: phoneNumber.Value,
                    RegisteredAt: customer.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register customer with email: {Email}", request.Email);
                throw;
            }
        }
    }
}
