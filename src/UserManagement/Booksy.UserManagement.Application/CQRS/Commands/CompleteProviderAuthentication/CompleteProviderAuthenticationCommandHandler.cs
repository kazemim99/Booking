// ========================================
// CompleteProviderAuthenticationCommandHandler.cs
// Unified handler for provider phone verification and authentication
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.CQRS.Commands.VerifyPhone;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.CompleteProviderAuthentication;

/// <summary>
/// Handler for completing provider authentication via phone verification
/// Supports both new provider registration and existing provider login
/// </summary>
public sealed class CompleteProviderAuthenticationCommandHandler
    : ICommandHandler<CompleteProviderAuthenticationCommand, CompleteProviderAuthenticationResponse>
{
    private readonly ISender _mediator;
    private readonly IPhoneVerificationRepository _verificationRepo;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProviderInfoService _providerInfoService;
    private readonly IPersonProvisioningService _personProvisioningService;
    private readonly Abstractions.Persistence.IUserManagementUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteProviderAuthenticationCommandHandler> _logger;

    public CompleteProviderAuthenticationCommandHandler(
        ISender mediator,
        IPhoneVerificationRepository verificationRepo,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IProviderInfoService providerInfoService,
        IPersonProvisioningService personProvisioningService,
        Abstractions.Persistence.IUserManagementUnitOfWork unitOfWork,
        ILogger<CompleteProviderAuthenticationCommandHandler> logger)
    {
        _mediator = mediator;
        _verificationRepo = verificationRepo;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _providerInfoService = providerInfoService;
        _personProvisioningService = personProvisioningService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompleteProviderAuthenticationResponse> Handle(
        CompleteProviderAuthenticationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting provider authentication for phone: {Phone}",
            MaskPhoneNumber(request.PhoneNumber));

        // Step 1: Verify the phone number and code
        var phoneNumber = PhoneNumber.From(request.PhoneNumber);
        var verification = await _verificationRepo.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (verification == null)
        {
            throw new InvalidOperationException("No verification found for this phone number");
        }

        // Verify the code using existing command
        var verifyCommand = new VerifyPhoneCommand(
            VerificationId: verification.Id.Value,
            Code: request.Code);

        var verifyResult = await _mediator.Send(verifyCommand, cancellationToken);

        if (!verifyResult.Success)
        {
            throw new InvalidOperationException(verifyResult.Message);
        }

        _logger.LogInformation("Phone verification successful for: {Phone}", MaskPhoneNumber(request.PhoneNumber));

        // Step 2: Resolve the Person for this phone through the single guarded path.
        // ONE PERSON PER PHONE: an existing account is reused and granted the Provider
        // capacity (a customer signing in here becomes Type=Both) instead of being
        // rejected — which previously pushed the same human into a second account.
        var provisioning = await _personProvisioningService.GetOrCreateByPhoneAsync(
            phoneNumber,
            UserType.Provider,
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);

        var user = provisioning.Person;
        var isNewUser = provisioning.IsNewPerson;

        _logger.LogInformation(
            "Resolved provider person {UserId} (new: {IsNew}, capacity granted: {Granted})",
            user.Id.Value, isNewUser, provisioning.CapacityGranted);

        // Blocked/inactive accounts must not obtain tokens via OTP. The password
        // path enforces this in User.Authenticate(), but the OTP flow issues tokens
        // directly and would otherwise bypass it. Deleted users never reach here —
        // the DbContext query filter excludes them.
        if (user.Status is UserStatus.Banned or UserStatus.Suspended or UserStatus.Inactive)
        {
            _logger.LogWarning(
                "Rejected OTP sign-in for {Status} provider account. UserId: {UserId}",
                user.Status, user.Id.Value);

            throw new InvalidOperationException(
                $"This account is {user.Status} and cannot sign in. Please contact support.");
        }

        // Step 3: Query provider information from ServiceCatalog
        string? providerId = null;
        string? providerStatus = null;
        bool requiresOnboarding = true;

        if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
        {
            _logger.LogInformation("Querying provider info for UserId: {UserId}", user.Id);

            try
            {
                var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
                    user.Id.Value,
                    cancellationToken);

                if (providerInfo != null)
                {
                    providerId = providerInfo.ProviderId.ToString();
                    providerStatus = providerInfo.Status;
                    requiresOnboarding = providerStatus == "Pending";

                    _logger.LogInformation(
                        "Provider found: ProviderId={ProviderId}, Status={Status}",
                        providerId,
                        providerStatus);
                }
                else
                {
                    _logger.LogInformation(
                        "No provider profile found for UserId: {UserId} - needs onboarding",
                        user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error querying provider info for UserId: {UserId}, continuing without provider claims",
                    user.Id);
            }
        }

        // Step 4: Generate authentication tokens
        var roles = user.Roles.Select(r => r.Name).ToList();
        var displayName = user.Profile.GetFullName();

        var accessToken = _jwtTokenService.GenerateAccessToken(
            userId: user.Id,
            userType: UserType.Provider,
            email: user.Email,
            displayName: displayName,
            firstName: user.Profile.FirstName,
            lastName: user.Profile.LastName,
            status: user.Status.ToString(),
            roles: roles,
            providerId: providerId,
            providerStatus: providerStatus,
            phoneNumber: user.PhoneNumber?.Value,
            expirationHours: 24
        );

        // Generate refresh token
        var refreshToken = Domain.Entities.RefreshToken.Generate(
            expirationDays: 30,
            createdByIp: request.IpAddress);

        user.AddRefreshToken(refreshToken);
        await _userRepository.SaveAsync(user, cancellationToken);
        // Persist explicitly: SaveAsync only tracks, and the generic TransactionBehavior
        // commits the ServiceCatalog unit of work (DI last-wins), not UserManagement —
        // so without this the new provider user is never saved (its token then points at
        // a non-existent user and later lookups, e.g. registration step-9, 500).
        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Provider authentication completed successfully. UserId: {UserId}, ProviderId: {ProviderId}, IsNew: {IsNew}",
            user.Id.Value,
            providerId ?? "none",
            isNewUser);

        return new CompleteProviderAuthenticationResponse
        {
            IsNewProvider = isNewUser,
            UserId = user.Id.Value,
            ProviderId = providerId != null ? Guid.Parse(providerId) : null,
            ProviderStatus = providerStatus,
            PhoneNumber = phoneNumber.Value,
            Email = user.Email.Value,
            FullName = displayName,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 86400, // 24 hours
            TokenType = "Bearer",
            RequiresOnboarding = requiresOnboarding,
            Message = isNewUser
                ? "Welcome! Please complete your provider profile."
                : requiresOnboarding
                    ? "Welcome back! Please complete your provider onboarding."
                    : "Welcome back! You're now logged in."
        };
    }



    /// <summary>
    /// Masks phone number for logging (shows only last 4 digits)
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        return $"****{phoneNumber[^4..]}";
    }
}
