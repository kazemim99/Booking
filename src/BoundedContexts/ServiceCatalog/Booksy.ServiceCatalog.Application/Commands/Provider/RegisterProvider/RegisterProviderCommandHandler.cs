// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    public sealed class RegisterProviderCommandHandler : ICommandHandler<RegisterProviderCommand, RegisterProviderResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderRegistrationService _registrationService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<RegisterProviderCommandHandler> _logger;

        public RegisterProviderCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            IProviderRegistrationService registrationService,
            ITokenService tokenService,
            ILogger<RegisterProviderCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _registrationService = registrationService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<RegisterProviderResult> Handle(
            RegisterProviderCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering new provider for owner: {OwnerId}", request.OwnerId);

            // Create value objects FIRST. This must run before any DB-backed business-state check
            // (uniqueness, ownership): a malformed request should fail on its own shape with a 400,
            // never be shadowed by an unrelated 409/500 from a check the request would never reach in
            // a well-formed call. It also means a bad request costs no round trip. (Found via
            // docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 4: RegisterProvider_WithInvalidEmail_ShouldReturn400BadRequest
            // flaked to 500 under the merged suite's load whenever an earlier test's "Test Salon"
            // provider was still on disk — the business-name check ran first and threw before
            // Email.Create ever got a chance to report the real problem.)
            var email = String.IsNullOrEmpty(request.Email) ? null : Email.Create(request.Email);
            var primaryPhone = PhoneNumber.From(request.PrimaryPhone);
            var secondaryPhone = !string.IsNullOrEmpty(request.SecondaryPhone)
                ? PhoneNumber.From(request.SecondaryPhone)
                : null;

            var contactInfo = ContactInfo.Create(
                email,
                primaryPhone,
                secondaryPhone,
                request.Website);

            var formattedAddress = $"{request.Street}, {request.City}, {request.State}";

            var address = BusinessAddress.Create(
                formattedAddress,
                request.Street,
                request.City,
                request.State,
                request.PostalCode,
                request.Country,
                null, // ProvinceId
                null, // CityId
                request.Latitude,
                request.Longitude);

            // Validate business rules (name uniqueness, owner eligibility, address) now that the
            // request itself is known to be well-formed.
            await _registrationService.ValidateRegistrationAsync(request, cancellationToken);

            // Check if owner already has a provider
            var ownerId = UserId.From(request.OwnerId);
            var existingProvider = await _providerReadRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
            if (existingProvider != null)
            {
                // A real conflict, not a malformed request or a server fault → 409, matching
                // ValidateRegistrationAsync's own "name already taken" conflict below.
                throw new ConflictException($"User {request.OwnerId} already has a registered provider");
            }

            // Create provider aggregate
            var provider = ServiceCatalog.Domain.Aggregates.Provider.RegisterProvider(
                ownerId,
                request.BusinessName,
                request.Description,
                request.PrimaryCategory,
                contactInfo,
                address);

            // Save to repository
            await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

            _logger.LogInformation("Provider registered successfully. ProviderId: {ProviderId}", provider.Id);

            // Generate new token with provider claims
            var tokenResponse = await _tokenService.GenerateTokenWithProviderClaimsAsync(
                request.OwnerId,
                provider.Id.Value,
                provider.Status.ToString(),
                cancellationToken);

            _logger.LogInformation(
                "Generated new token for user {UserId} with provider {ProviderId}",
                request.OwnerId,
                provider.Id.Value);

            return new RegisterProviderResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                PrimaryCategory: provider.PrimaryCategory,
                Status: provider.Status,
                RegisteredAt: provider.RegisteredAt,
                AccessToken: tokenResponse.AccessToken,
                RefreshToken: tokenResponse.RefreshToken,
                ExpiresIn: tokenResponse.ExpiresIn);
        }
    }
}