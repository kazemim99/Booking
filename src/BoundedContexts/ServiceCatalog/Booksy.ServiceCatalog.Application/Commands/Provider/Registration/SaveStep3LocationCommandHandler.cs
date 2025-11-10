using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep3LocationCommandHandler
    : ICommandHandler<SaveStep3LocationCommand, SaveStep3LocationResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<SaveStep3LocationCommandHandler> _logger;

    public SaveStep3LocationCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITokenService tokenService,
        ILogger<SaveStep3LocationCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<SaveStep3LocationResult> Handle(
        SaveStep3LocationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        // Check if user already has a draft provider
        var existingProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        if (existingProvider != null)
        {
            // Update existing draft with location information
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
                null, // ProvinceId
                null, // CityId
                (double)request.Latitude,
                (double)request.Longitude);

            var contactInfo = ContactInfo.Create(
                Email.Create(request.Email),
                PhoneNumber.Create(request.PhoneNumber));

            existingProvider.UpdateBusinessProfile(
                request.BusinessName,
                request.BusinessDescription, "\"profileImageUrl\"");

            existingProvider.UpdateContactInfo(contactInfo);
            existingProvider.UpdateAddress(address);
            existingProvider.UpdateRegistrationStep(3);

            await _unitOfWork.CommitAsync(cancellationToken);

            // Generate new token with provider claims
            var tokenResponse = await _tokenService.GenerateTokenWithProviderClaimsAsync(
                userId.Value,
                existingProvider.Id.Value,
                existingProvider.Status.ToString(),
                cancellationToken);

            _logger.LogInformation(
                "Updated provider draft and generated new token for user {UserId} with provider {ProviderId}",
                userId.Value,
                existingProvider.Id.Value);

            return new SaveStep3LocationResult(
                existingProvider.Id.Value,
                3,
                "Location information updated successfully",
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                tokenResponse.ExpiresIn);
        }

        // Parse category to ProviderType enum
        if (!Enum.TryParse<ProviderType>(request.Category, true, out var providerType))
        {
            throw new InvalidOperationException($"Invalid category: {request.Category}");
        }

        // Create contact info
        var newContactInfo = ContactInfo.Create(
            Email.Create(request.Email),
            PhoneNumber.Create(request.PhoneNumber));

        // Create address
        var newStreet = string.IsNullOrWhiteSpace(request.AddressLine2)
            ? request.AddressLine1
            : $"{request.AddressLine1}, {request.AddressLine2}";

        var newFormattedAddress = $"{newStreet}, {request.City}, {request.Province}";

        var newAddress = BusinessAddress.Create(
            newFormattedAddress,
            newStreet,
            request.City,
            request.Province,
            request.PostalCode,
            "IR",
            null, // ProvinceId
            null, // CityId
            (double)request.Latitude,
            (double)request.Longitude);

        // Create new draft provider
        var provider = Domain.Aggregates.Provider.CreateDraft(
            userId,
            request.BusinessName,
            request.BusinessDescription,
            providerType,
            newContactInfo,
            newAddress,
            registrationStep: 3);

        await _providerRepository.SaveProviderAsync(provider, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Generate new token with provider claims
        var newTokenResponse = await _tokenService.GenerateTokenWithProviderClaimsAsync(
            userId.Value,
            provider.Id.Value,
            provider.Status.ToString(),
            cancellationToken);

        _logger.LogInformation(
            "Created provider draft and generated new token for user {UserId} with provider {ProviderId}",
            userId.Value,
            provider.Id.Value);

        return new SaveStep3LocationResult(
            provider.Id.Value,
            3,
            "Provider draft created successfully",
            newTokenResponse.AccessToken,
            newTokenResponse.RefreshToken,
            newTokenResponse.ExpiresIn);
    }
}
