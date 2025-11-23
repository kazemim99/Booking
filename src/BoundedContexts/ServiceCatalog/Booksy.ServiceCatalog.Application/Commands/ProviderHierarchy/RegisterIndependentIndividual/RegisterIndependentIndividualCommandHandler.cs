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

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RegisterIndependentIndividual
{
    public sealed class RegisterIndependentIndividualCommandHandler
        : ICommandHandler<RegisterIndependentIndividualCommand, RegisterIndependentIndividualResult>
    {
        private readonly IProviderWriteRepository _providerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RegisterIndependentIndividualCommandHandler> _logger;

        public RegisterIndependentIndividualCommandHandler(
            IProviderWriteRepository providerRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<RegisterIndependentIndividualCommandHandler> logger)
        {
            _providerRepository = providerRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<RegisterIndependentIndividualResult> Handle(
            RegisterIndependentIndividualCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering new independent individual provider: {Name}",
                $"{request.FirstName} {request.LastName}");

            // 1. Get current user ID
            var userId = UserId.From(_currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated"));

            // 2. Check if user already has a draft provider
            var existingProvider = await _providerRepository
                .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

            if (existingProvider != null)
            {
                return new RegisterIndependentIndividualResult(
                    existingProvider.Id.Value,
                    existingProvider.HierarchyType.ToString(),
                    existingProvider.IsIndependent,
                    existingProvider.RegistrationStep,
                    "Draft provider already exists. Resuming registration.");
            }

            // 3. Map category string to ProviderType enum
            if (!Enum.TryParse<ProviderType>(request.Category, true, out var providerType))
            {
                throw new InvalidOperationException($"Invalid category: {request.Category}");
            }

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

            // 5. Create individual provider (draft)
            var provider = Domain.Aggregates.Provider.CreateDraft(
                userId,
                request.FirstName,
                request.LastName,
                request.BusinessName,
                request.BusinessDescription,
                providerType,
                contactInfo,
                address,
                ProviderHierarchyType.Individual,
                registrationStep: 3);

            // 6. Save
            await _providerRepository.SaveProviderAsync(provider, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Independent individual provider created: {ProviderId}", provider.Id.Value);

            return new RegisterIndependentIndividualResult(
                provider.Id.Value,
                provider.HierarchyType.ToString(),
                provider.IsIndependent,
                provider.RegistrationStep,
                "Independent individual provider created successfully");
        }
    }
}
