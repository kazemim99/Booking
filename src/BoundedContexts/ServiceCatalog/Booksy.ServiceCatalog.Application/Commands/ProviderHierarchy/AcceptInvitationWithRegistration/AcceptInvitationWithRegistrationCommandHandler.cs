using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitationWithRegistration;

/// <summary>
/// Handler for accepting invitations with quick registration for unregistered users
/// </summary>
public sealed class AcceptInvitationWithRegistrationCommandHandler
    : ICommandHandler<AcceptInvitationWithRegistrationCommand, AcceptInvitationWithRegistrationResult>
{
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
    private readonly IInvitationRegistrationService _registrationService;
    private readonly IDataCloningService _dataCloningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptInvitationWithRegistrationCommandHandler> _logger;

    public AcceptInvitationWithRegistrationCommandHandler(
        IProviderReadRepository providerReadRepository,
        IProviderWriteRepository providerWriteRepository,
        IProviderInvitationReadRepository invitationReadRepository,
        IProviderInvitationWriteRepository invitationWriteRepository,
        IInvitationRegistrationService registrationService,
        IDataCloningService dataCloningService,
        IUnitOfWork unitOfWork,
        ILogger<AcceptInvitationWithRegistrationCommandHandler> logger)
    {
        _providerReadRepository = providerReadRepository;
        _providerWriteRepository = providerWriteRepository;
        _invitationReadRepository = invitationReadRepository;
        _invitationWriteRepository = invitationWriteRepository;
        _registrationService = registrationService;
        _dataCloningService = dataCloningService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AcceptInvitationWithRegistrationResult> Handle(
        AcceptInvitationWithRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing invitation acceptance with registration for invitation {InvitationId}",
            request.InvitationId);

        // 1. Get and validate invitation
        var invitation = await _invitationReadRepository.GetByIdAsync(
            request.InvitationId,
            cancellationToken);

        if (invitation == null)
            throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

        if (invitation.Status != InvitationStatus.Pending)
            throw new DomainValidationException(
                $"Invitation is no longer pending (status: {invitation.Status})");

        if (invitation.OrganizationId.Value != request.OrganizationId)
            throw new DomainValidationException(
                $"Invitation belongs to different organization");

        // Validate invitation phone number matches
        if (invitation.PhoneNumber.Value != request.PhoneNumber)
            throw new DomainValidationException(
                "Phone number does not match invitation");

        // 2. Verify OTP code
        var otpValid = await _registrationService.VerifyOtpAsync(
            request.PhoneNumber,
            request.OtpCode,
            cancellationToken);

        if (!otpValid)
            throw new DomainValidationException("Invalid or expired OTP code");

        _logger.LogInformation("OTP verification successful for {PhoneNumber}", request.PhoneNumber);

        // 3. Create user account (this also checks for duplicate phone number)
        var userId = await _registrationService.CreateUserWithPhoneAsync(
            phoneNumber: request.PhoneNumber,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            cancellationToken);

        _logger.LogInformation("User account created with ID {UserId}", userId);

        // 4. Get organization provider ID (needed before creating individual provider)
        var organizationProviderId = invitation.OrganizationId;

        // 5. Create individual provider profile
        var providerId = await _registrationService.CreateIndividualProviderAsync(
            userId: userId,
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber,
            email: request.Email,
            organizationId: organizationProviderId,
            cancellationToken);

        _logger.LogInformation("Individual provider profile created with ID {ProviderId}", providerId);

        // 6. Clone organization data to new provider profile
        int clonedServicesCount = 0;
        int clonedWorkingHoursCount = 0;
        int clonedGalleryCount = 0;

        if (request.CloneServices)
        {
            clonedServicesCount = await _dataCloningService.CloneServicesAsync(
                sourceProviderId: organizationProviderId,
                targetProviderId: providerId,
                cancellationToken);

            _logger.LogInformation(
                "Cloned {Count} services from organization {OrgId} to provider {ProviderId}",
                clonedServicesCount, organizationProviderId, providerId);
        }

        if (request.CloneWorkingHours)
        {
            clonedWorkingHoursCount = await _dataCloningService.CloneWorkingHoursAsync(
                sourceProviderId: organizationProviderId,
                targetProviderId: providerId,
                cancellationToken);

            _logger.LogInformation(
                "Cloned {Count} working hours from organization {OrgId} to provider {ProviderId}",
                clonedWorkingHoursCount, organizationProviderId, providerId);
        }

        if (request.CloneGallery)
        {
            clonedGalleryCount = await _dataCloningService.CloneGalleryAsync(
                sourceProviderId: organizationProviderId,
                targetProviderId: providerId,
                markAsCloned: true,
                cancellationToken);

            _logger.LogInformation(
                "Cloned {Count} gallery images from organization {OrgId} to provider {ProviderId}",
                clonedGalleryCount, organizationProviderId, providerId);
        }

        // 7. Accept invitation (provider is already linked in CreateIndividualProviderAsync)
        invitation.Accept(providerId);
        await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);
        _logger.LogInformation(
            "Invitation {InvitationId} accepted. New provider {ProviderId} linked to organization {OrganizationId}",
            invitation.Id, providerId, organizationProviderId);

        // 8. Generate JWT tokens
        var userEmail = string.IsNullOrWhiteSpace(request.Email)
            ? $"{request.PhoneNumber.Replace("+", "")}@booksy.temp"
            : request.Email;

        var displayName = $"{request.FirstName} {request.LastName}";

        var tokens = await _registrationService.GenerateAuthTokensAsync(
            userId: userId,
            providerId: providerId,
            email: userEmail,
            displayName: displayName,
            cancellationToken);

        var accessToken = tokens.AccessToken;
        var refreshToken = tokens.RefreshToken;

        return new AcceptInvitationWithRegistrationResult(
            UserId: userId,
            ProviderId: providerId.Value,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ClonedServicesCount: clonedServicesCount,
            ClonedWorkingHoursCount: clonedWorkingHoursCount,
            ClonedGalleryCount: clonedGalleryCount,
            AcceptedAt: invitation.RespondedAt!.Value);
    }
}
