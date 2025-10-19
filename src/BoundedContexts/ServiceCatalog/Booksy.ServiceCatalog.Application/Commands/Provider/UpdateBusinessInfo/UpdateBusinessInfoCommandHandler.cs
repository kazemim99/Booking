using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessInfo;

public sealed class UpdateBusinessInfoCommandHandler : ICommandHandler<UpdateBusinessInfoCommand, UpdateBusinessInfoResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ILogger<UpdateBusinessInfoCommandHandler> _logger;

    public UpdateBusinessInfoCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        IProviderReadRepository providerReadRepository,
        ILogger<UpdateBusinessInfoCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _providerReadRepository = providerReadRepository;
        _logger = logger;
    }

    public async Task<UpdateBusinessInfoResult> Handle(
        UpdateBusinessInfoCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating business info for provider {ProviderId}",
            request.ProviderId);

        // Validate request
        ValidateRequest(request);

        // Get provider
        var providerId = Domain.ValueObjects.ProviderId.Create(request.ProviderId);
        var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
        }

        // Update business profile
        provider.UpdateBusinessProfile(
            request.BusinessName,
            request.Description);

        // Update contact info if email provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = Email.Create(request.Email);
            var phone = PhoneNumber.Create(request.PhoneNumber);
            var website = !string.IsNullOrWhiteSpace(request.Website)
                ? request.Website
                : null;

            var contactInfo = ContactInfo.Create(email, phone, null, website);
            provider.UpdateContactInfo(contactInfo);
        }

        // Save changes
        await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Business info updated successfully for provider {ProviderId}",
            request.ProviderId);

        return new UpdateBusinessInfoResult(
            provider.Id.Value,
            provider.Profile.BusinessName,
            request.OwnerFirstName,
            request.OwnerLastName,
            DateTime.UtcNow);
    }

    private void ValidateRequest(UpdateBusinessInfoCommand request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.BusinessName))
        {
            errors["businessName"] = new List<string> { "Business name is required" };
        }

        if (request.BusinessName?.Length > 200)
        {
            errors["businessName"] = new List<string> { "Business name cannot exceed 200 characters" };
        }

        if (string.IsNullOrWhiteSpace(request.OwnerFirstName))
        {
            errors["ownerFirstName"] = new List<string> { "Owner first name is required" };
        }

        if (string.IsNullOrWhiteSpace(request.OwnerLastName))
        {
            errors["ownerLastName"] = new List<string> { "Owner last name is required" };
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors["phoneNumber"] = new List<string> { "Phone number is required" };
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
        {
            errors["email"] = new List<string> { "Invalid email format" };
        }

        if (!string.IsNullOrWhiteSpace(request.Website) && !IsValidUrl(request.Website))
        {
            errors["website"] = new List<string> { "Invalid website URL format" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
