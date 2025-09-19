﻿// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
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
        private readonly ILogger<RegisterProviderCommandHandler> _logger;

        public RegisterProviderCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            IProviderRegistrationService registrationService,
            ILogger<RegisterProviderCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _registrationService = registrationService;
            _logger = logger;
        }

        public async Task<RegisterProviderResult> Handle(
            RegisterProviderCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering new provider for owner: {OwnerId}", request.OwnerId);

            // Validate business rules
            await _registrationService.ValidateRegistrationAsync(request, cancellationToken);

            // Check if owner already has a provider
            var ownerId = UserId.From(request.OwnerId);
            var existingProvider = await _providerReadRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
            if (existingProvider != null)
            {
                throw new InvalidOperationException($"User {request.OwnerId} already has a registered provider");
            }

            // Create value objects
            var email = Email.From(request.Email);
            var primaryPhone = PhoneNumber.From(request.PrimaryPhone);
            var secondaryPhone = !string.IsNullOrEmpty(request.SecondaryPhone)
                ? PhoneNumber.From(request.SecondaryPhone)
                : null;

            var contactInfo = ContactInfo.Create(
                email,
                primaryPhone,
                secondaryPhone,
                request.Website);

            var address = BusinessAddress.Create(
                request.Street,
                request.City,
                request.State,
                request.PostalCode,
                request.Country,
                request.Latitude,
                request.Longitude);

            // Create provider aggregate
            var provider = ServiceCatalog.Domain.Aggregates.Provider.RegisterProvider(
                ownerId,
                request.BusinessName,
                request.Description,
                request.ProviderType,
                contactInfo,
                address);

            // Save to repository
            await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

            _logger.LogInformation("Provider registered successfully. ProviderId: {ProviderId}", provider.Id);

            return new RegisterProviderResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                Status: provider.Status,
                RegisteredAt: provider.RegisteredAt);
        }
    }
}