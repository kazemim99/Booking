using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RegisterOrganizationProvider
{
    public sealed class RegisterOrganizationProviderCommandHandler
        : ICommandHandler<RegisterOrganizationProviderCommand, RegisterOrganizationProviderResult>
    {
        private readonly IProviderWriteRepository _providerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RegisterOrganizationProviderCommandHandler> _logger;

        public RegisterOrganizationProviderCommandHandler(
            IProviderWriteRepository providerRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<RegisterOrganizationProviderCommandHandler> logger)
        {
            _providerRepository = providerRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<RegisterOrganizationProviderResult> Handle(
            RegisterOrganizationProviderCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering new organization provider: {BusinessName}", request.BusinessName);

            // 1. Get current user ID
            var userId = UserId.From(_currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated"));

            // 2. Check if user already has a draft provider
            var existingProvider = await _providerRepository
                .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

            if (existingProvider != null)
            {
                return new RegisterOrganizationProviderResult(
                    existingProvider.Id.Value,
                    existingProvider.HierarchyType.ToString(),
                    existingProvider.RegistrationStep,
                    "Draft provider already exists. Resuming registration.");
            }

          var providerType =  MapCategoryToProviderType(request.Category);


          
            // 4. Create value objects
            var contactInfo = ContactInfo.Create(
                Email.Create(request.Email),
                PhoneNumber.Create(request.PhoneNumber));

            var street = string.IsNullOrWhiteSpace(request.AddressLine2)
                ? request.AddressLine1
                : $"{request.AddressLine1}, {request.AddressLine2}";

            var formattedAddress = $"{street}, {request.City}, {request.Province}";

            var address = BusinessAddress.Create(
                formattedAddress,
                street,
                request.City,
                request.Province,
                request.PostalCode,
                "IR",
                null,
                null,
                (double)request.Latitude,
                (double)request.Longitude);

            // 5. Create organization provider (draft)
            var provider = Domain.Aggregates.Provider.CreateDraft(
                userId,
                request.OwnerFirstName,
                request.OwnerLastName,
                request.BusinessName,
                request.BusinessDescription,
                providerType,
                contactInfo,
                address,
                ProviderHierarchyType.Organization,
                registrationStep: 3);

            // 6. Save
            await _providerRepository.SaveProviderAsync(provider, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Organization provider created: {ProviderId}", provider.Id.Value);

            return new RegisterOrganizationProviderResult(
                provider.Id.Value,
                provider.HierarchyType.ToString(),
                provider.RegistrationStep,
                "Organization provider created successfully");
        }


        private ProviderType MapCategoryToProviderType(string categoryId)
        {
            return categoryId.ToLowerInvariant() switch
            {
                "nail_salon" => ProviderType.Salon,
                "hair_salon" => ProviderType.Salon,
                "brows_lashes" => ProviderType.Salon,
                "braids_locs" => ProviderType.Salon,
                "massage" => ProviderType.Spa,
                "barbershop" => ProviderType.Salon,
                "aesthetic_medicine" => ProviderType.Medical,
                "dental_orthodontics" => ProviderType.Medical,
                "hair_removal" => ProviderType.Spa,
                "health_fitness" => ProviderType.GymFitness,
                "home_services" => ProviderType.HomeServices,
                _ => ProviderType.Salon // Default
            };
        }
    }
}
