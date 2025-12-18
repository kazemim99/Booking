using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitationWithRegistration;

/// <summary>
/// Handler for accepting invitations with quick registration for unregistered users
/// Implements SAGA pattern with compensation for distributed transaction management
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

        // ==========================================
        // VALIDATION PHASE (Read-only, outside transaction)
        // ==========================================

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

        // ==========================================
        // SAGA PATTERN: Track state for compensation
        // ==========================================
        UserId? createdUserId = null;
        ProviderAggregate? createdProvider = null;
        bool userCreationSucceeded = false;
        int clonedServicesCount = 0;
        int clonedWorkingHoursCount = 0;
        int clonedGalleryCount = 0;

        try
        {
            // ==========================================
            // SAGA STEP 1: Create user account
            // (External API call - cannot be rolled back via DB transaction)
            // ==========================================
            createdUserId = await _registrationService.CreateUserWithPhoneAsync(
                phoneNumber: request.PhoneNumber,
                firstName: request.FirstName,
                lastName: request.LastName,
                email: request.Email,
                cancellationToken);

            userCreationSucceeded = true;
            _logger.LogInformation("✓ SAGA STEP 1: User account created with ID {UserId}", createdUserId);

            var organizationProviderId = invitation.OrganizationId;

            // ==========================================
            // SAGA STEPS 2-5: Wrap in database transaction
            // (These will auto-rollback if any step fails)
            // ==========================================
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // SAGA STEP 2: Create individual provider profile
                createdProvider = await _registrationService.CreateIndividualProviderAsync(
                    userId: createdUserId,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    phoneNumber: request.PhoneNumber,
                    email: request.Email,
                    organizationId: organizationProviderId,
                    cancellationToken);

                _logger.LogInformation("✓ SAGA STEP 2: Individual provider profile created with ID {ProviderId}", createdProvider.Id);

                // SAGA STEP 3: Clone organization data to new provider profile
                if (request.CloneServices)
                {
                    clonedServicesCount = await _dataCloningService.CloneServicesAsync(
                        sourceProviderId: organizationProviderId,
                        targetProvider: createdProvider,
                        cancellationToken);

                    _logger.LogInformation(
                        "✓ SAGA STEP 3a: Cloned {Count} services from organization {OrgId} to provider {ProviderId}",
                        clonedServicesCount, organizationProviderId, createdProvider.Id);
                }

                if (request.CloneWorkingHours)
                {
                    clonedWorkingHoursCount = await _dataCloningService.CloneWorkingHoursAsync(
                        sourceProviderId: organizationProviderId,
                        targetProvider: createdProvider,
                        cancellationToken);

                    _logger.LogInformation(
                        "✓ SAGA STEP 3b: Cloned {Count} working hours from organization {OrgId} to provider {ProviderId}",
                        clonedWorkingHoursCount, organizationProviderId, createdProvider.Id);
                }

                if (request.CloneGallery)
                {
                    clonedGalleryCount = await _dataCloningService.CloneGalleryAsync(
                        sourceProviderId: organizationProviderId,
                        targetProviderId: createdProvider,
                        markAsCloned: true,
                        cancellationToken);

                    _logger.LogInformation(
                        "✓ SAGA STEP 3c: Cloned {Count} gallery images from organization {OrgId} to provider {ProviderId}",
                        clonedGalleryCount, organizationProviderId, createdProvider.Id);
                }

                // SAGA STEP 4: Save all cloned data to database
                // Note: SaveChangesAsync is called automatically by ExecuteInTransactionAsync
                _logger.LogInformation(
                    "✓ SAGA STEP 4: Persisted cloned data for provider {ProviderId}: {Services} services, {Hours} working hours, {Gallery} gallery items",
                    createdProvider.Id, clonedServicesCount, clonedWorkingHoursCount, clonedGalleryCount);

                // SAGA STEP 5: Accept invitation
                invitation.Accept(createdProvider.Id);
                await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);
                _logger.LogInformation(
                    "✓ SAGA STEP 5: Invitation {InvitationId} accepted. New provider {ProviderId} linked to organization {OrganizationId}",
                    invitation.Id, createdProvider.Id, organizationProviderId);

                return (createdProvider, clonedServicesCount, clonedWorkingHoursCount, clonedGalleryCount);
            }, cancellationToken);

            // Transaction succeeded, extract results
            createdProvider = result.createdProvider;
            clonedServicesCount = result.clonedServicesCount;
            clonedWorkingHoursCount = result.clonedWorkingHoursCount;
            clonedGalleryCount = result.clonedGalleryCount;

            // ==========================================
            // SAGA STEP 6: Generate JWT tokens (outside transaction)
            // ==========================================
            var userEmail = string.IsNullOrWhiteSpace(request.Email)
                ? $"{request.PhoneNumber.Replace("+", "")}@booksy.temp"
                : request.Email;

            var displayName = $"{request.FirstName} {request.LastName}";

            var tokens = await _registrationService.GenerateAuthTokensAsync(
                userId: createdUserId,
                providerId: createdProvider.Id,
                email: userEmail,
                displayName: displayName,
                cancellationToken);

            _logger.LogInformation("✓ SAGA COMPLETED: All steps successful for user {UserId}, provider {ProviderId}",
                createdUserId, createdProvider.Id);

            return new AcceptInvitationWithRegistrationResult(
                UserId: createdUserId,
                ProviderId: createdProvider.Id.Value,
                AccessToken: tokens.AccessToken,
                RefreshToken: tokens.RefreshToken,
                ClonedServicesCount: clonedServicesCount,
                ClonedWorkingHoursCount: clonedWorkingHoursCount,
                ClonedGalleryCount: clonedGalleryCount,
                AcceptedAt: invitation.RespondedAt!.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "✗ SAGA FAILED: Error during invitation acceptance. UserCreated: {UserCreated}, UserId: {UserId}, ProviderId: {ProviderId}",
                userCreationSucceeded, createdUserId, createdProvider?.Id);

            // ==========================================
            // COMPENSATION: Rollback external changes
            // ==========================================
            // NOTE: Database changes are automatically rolled back by the transaction
            // We only need to compensate for the user creation (external API call)

            if (userCreationSucceeded && createdUserId != null)
            {
                _logger.LogWarning(
                    "⚠ COMPENSATION: Attempting to delete orphaned user {UserId} due to failed registration",
                    createdUserId);

                try
                {
                    var compensationSucceeded = await _registrationService.DeleteUserAsync(
                        createdUserId,
                        $"Invitation acceptance failed: {ex.Message}",
                        cancellationToken);

                    if (compensationSucceeded)
                    {
                        _logger.LogInformation(
                            "✓ COMPENSATION SUCCESS: Orphaned user {UserId} deleted successfully",
                            createdUserId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠ COMPENSATION PARTIAL: User {UserId} deletion failed. MANUAL CLEANUP REQUIRED!",
                            createdUserId);
                    }
                }
                catch (Exception compensationEx)
                {
                    _logger.LogError(compensationEx,
                        "✗ COMPENSATION FAILED: Could not delete orphaned user {UserId}. MANUAL CLEANUP REQUIRED!",
                        createdUserId);
                    // Don't throw - we want to surface the original exception
                }
            }

            // Re-throw original exception to inform caller
            throw;
        }
    }
}
