// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/Implementations/ProviderRegistrationService.cs
// ========================================
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Services.Implementations
{
    public sealed class ProviderRegistrationService : IProviderRegistrationService
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<ProviderRegistrationService> _logger;

        public ProviderRegistrationService(
            IProviderReadRepository providerRepository,
            ILogger<ProviderRegistrationService> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task ValidateRegistrationAsync(RegisterProviderCommand command, CancellationToken cancellationToken = default)
        {
            // Check if business name is unique. A real conflict, not a malformed request or a
            // server fault → ConflictException (409), the same mapping RegisterProviderCommandHandler
            // uses for "owner already has a provider" (ExceptionHandlingMiddleware has no case for a
            // bare InvalidOperationException, which used to fall through to an opaque 500).
            var isNameUnique = await IsBusinessNameAvailableAsync(command.BusinessName, cancellationToken);
            if (!isNameUnique)
            {
                throw new ConflictException($"Business name '{command.BusinessName}' is already taken");
            }

            // Check if owner is eligible
            var isOwnerEligible = await IsOwnerEligibleAsync(command.OwnerId, cancellationToken);
            if (!isOwnerEligible)
            {
                throw new ConflictException($"Owner {command.OwnerId} is not eligible to register a provider");
            }

            // Validate address — malformed input, not a conflict → 400.
            var isAddressValid = await ValidateBusinessAddressAsync(
                command.Street, command.City, command.State, command.PostalCode, command.Country, cancellationToken);
            if (!isAddressValid)
            {
                throw new DomainValidationException("Address", "Invalid business address provided");
            }
        }

        public async Task<bool> IsBusinessNameAvailableAsync(string businessName, CancellationToken cancellationToken = default)
        {
            return !await _providerRepository.ExistsByBusinessNameAsync(businessName, null, cancellationToken);
        }

        public async Task<bool> IsOwnerEligibleAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var existingProvider = await _providerRepository.GetByOwnerIdAsync(
                Core.Domain.ValueObjects.UserId.From(ownerId), cancellationToken);

            return existingProvider == null; // User can only have one provider
        }

        public async Task<bool> ValidateBusinessAddressAsync(
            string street, string city, string state, string postalCode, string country, CancellationToken cancellationToken = default)
        {
            // Basic validation - in a real system you might validate against a geocoding service
            return !string.IsNullOrWhiteSpace(street) &&
                   !string.IsNullOrWhiteSpace(city) &&
                   !string.IsNullOrWhiteSpace(country);
        }
    }
}