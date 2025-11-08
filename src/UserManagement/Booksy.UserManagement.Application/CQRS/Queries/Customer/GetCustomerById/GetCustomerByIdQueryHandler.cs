// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerById/GetCustomerByIdQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById
{
    public sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDetailsViewModel>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetCustomerByIdQueryHandler> _logger;

        public GetCustomerByIdQueryHandler(
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ILogger<GetCustomerByIdQueryHandler> logger)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CustomerDetailsViewModel> Handle(
            GetCustomerByIdQuery request,
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

                // Get user details for email
                var user = await _userRepository.GetByIdAsync(customer.UserId, cancellationToken);

                return new CustomerDetailsViewModel
                {
                    CustomerId = customer.Id.Value,
                    UserId = customer.UserId.Value,
                    Email = user?.Email.Value ?? string.Empty,
                    FirstName = customer.Profile.FirstName,
                    LastName = customer.Profile.LastName,
                    MiddleName = customer.Profile.MiddleName,
                    FullName = customer.Profile.GetFullName(),
                    DateOfBirth = customer.Profile.DateOfBirth,
                    Age = customer.Profile.GetAge(),
                    Gender = customer.Profile.Gender,
                    PhoneNumber = customer.Profile.PhoneNumber?.Value,
                    Address = customer.Profile.Address != null
                        ? new AddressViewModel(
                            customer.Profile.Address.Street,
                            customer.Profile.Address.City,
                            customer.Profile.Address.State,
                            customer.Profile.Address.PostalCode,
                            customer.Profile.Address.Country,
                            customer.Profile.Address.Unit)
                        : null,
                    AvatarUrl = customer.Profile.AvatarUrl,
                    Bio = customer.Profile.Bio,
                    IsActive = customer.IsActive,
                    FavoriteProvidersCount = customer.FavoriteProviders.Count,
                    CreatedAt = customer.CreatedAt,
                    LastModifiedAt = customer.LastModifiedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer by ID: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
