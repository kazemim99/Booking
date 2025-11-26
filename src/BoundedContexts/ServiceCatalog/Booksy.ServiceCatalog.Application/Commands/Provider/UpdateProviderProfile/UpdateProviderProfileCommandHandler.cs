using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderProfile;

public sealed class UpdateProviderProfileCommandHandler : ICommandHandler<UpdateProviderProfileCommand, UpdateProviderProfileResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly ILogger<UpdateProviderProfileCommandHandler> _logger;

    public UpdateProviderProfileCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        ILogger<UpdateProviderProfileCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _logger = logger;
    }

    public async Task<UpdateProviderProfileResult> Handle(
        UpdateProviderProfileCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating profile for provider {ProviderId}",
            request.ProviderId);

        // Get provider
        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerWriteRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Update profile image if provided
        if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
        {
            provider.Profile.UpdateProfileImage(request.ProfileImageUrl);
        }

        // Update contact info if email provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = Email.Create(request.Email);
            var existingPrimaryPhone = provider.ContactInfo?.PrimaryPhone ?? PhoneNumber.From("+10000000000");

            var contactInfo = ContactInfo.Create(
                email,
                existingPrimaryPhone,
                provider.ContactInfo?.SecondaryPhone,
                provider.Profile.Website);

            provider.UpdateContactInfo(contactInfo);
        }

        // Save changes
        await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Profile updated successfully for provider {ProviderId}",
            request.ProviderId);

        return new UpdateProviderProfileResult(true, "Profile updated successfully");
    }
}
