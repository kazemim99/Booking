// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/RegisterFromVerifiedPhone/RegisterFromVerifiedPhoneCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.RegisterFromVerifiedPhone;

/// <summary>
/// Handler for creating a user account from a verified phone number
/// </summary>
public sealed class RegisterFromVerifiedPhoneCommandHandler
    : ICommandHandler<RegisterFromVerifiedPhoneCommand, RegisterFromVerifiedPhoneResult>
{
    private readonly IPhoneVerificationRepository _verificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _tokenService;
    private readonly IProviderInfoService _providerInfoService;
    private readonly ILogger<RegisterFromVerifiedPhoneCommandHandler> _logger;

    public RegisterFromVerifiedPhoneCommandHandler(
        IPhoneVerificationRepository verificationRepository,
        IUserRepository userRepository,
        IJwtTokenService tokenService,
        IProviderInfoService providerInfoService,
        ILogger<RegisterFromVerifiedPhoneCommandHandler> logger)
    {
        _verificationRepository = verificationRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _providerInfoService = providerInfoService;
        _logger = logger;
    }

    public async Task<RegisterFromVerifiedPhoneResult> Handle(
        RegisterFromVerifiedPhoneCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate verification exists and is verified
        var verificationId = VerificationId.From(Guid.Parse(request.VerificationId));
        var verification = await _verificationRepository.GetByIdAsync(verificationId, cancellationToken);

        if (verification == null)
        {
            throw new DomainValidationException("VerificationId", "Phone verification not found");
        }

        if (verification.Status != VerificationStatus.Verified)
        {
            throw new DomainValidationException(
                "VerificationId",
                $"Phone verification is not verified. Current status: {verification.Status}");
        }

        // 2. Check if user with this phone already exists
        var phoneNumber = verification.PhoneNumber;
        var existingUser = await _userRepository.GetByPhoneNumberAsync(
            phoneNumber.Value,
            cancellationToken);

        User user;
        bool isNewUser = false;

        if (existingUser != null)
        {
            // User already exists, just return tokens
            user = existingUser;
            _logger.LogInformation(
                "User already exists for phone {Phone}, returning existing user",
                phoneNumber.Value);
        }
        else
        {
            // 3. Create new user with verified phone
            var firstName = request.FirstName ?? "User";
            var lastName = request.LastName ?? phoneNumber.NationalNumber;

            // Create user profile
            var profile = UserProfile.Create(
                firstName,
                lastName,
                middleName: null,
                dateOfBirth: null,
                gender: null);

            profile.UpdateContactInfo(phoneNumber, null, null);

            // Create email placeholder (phone@booksy.temp)
            // User can update this later in their profile
            var tempEmail = Email.Create($"{phoneNumber.NationalNumber}@booksy.temp");

            // Register user with phone (no password required)
            user = User.RegisterWithPhone(
                tempEmail,
                phoneNumber,
                profile,
                request.UserType);

            await _userRepository.SaveAsync(user, cancellationToken);
            isNewUser = true;

            _logger.LogInformation(
                "Created new user from verified phone. UserId: {UserId}, Phone: {Phone}",
                user.Id.Value,
                phoneNumber.Value);
        }

        // 4. Query provider information if user has Provider role
        string? providerId = null;
        string? providerStatus = null;
        if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
        {
            _logger.LogInformation("User has Provider role, querying provider info for UserId: {UserId}", user.Id);
            try
            {
                var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
                    user.Id.Value,
                    cancellationToken);

                if (providerInfo != null)
                {
                    providerId = providerInfo.ProviderId.ToString();
                    providerStatus = providerInfo.Status;
                    _logger.LogInformation("Provider found: ProviderId={ProviderId}, Status={Status}",
                        providerId, providerStatus);
                }
                else
                {
                    _logger.LogInformation("No provider found for UserId: {UserId} - new provider registration", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying provider info for UserId: {UserId}, continuing without provider claims", user.Id);
                // Continue without provider claims if there's an error
            }
        }

        // 5. Generate JWT tokens
        var displayName = $"{user.Profile.FirstName} {user.Profile.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = phoneNumber.NationalNumber;
        }

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Type,
            user.Email,
            displayName,
            user.Status.ToString(),
            user.Roles.Select(r => r.Name),
            providerId,
            providerStatus,
            expirationHours: 24);

        // Generate and add refresh token
        var refreshToken = RefreshToken.Generate(
            expirationDays: 30,
            createdByIp: request.IpAddress);
        user.AddRefreshToken(refreshToken);
        await _userRepository.SaveAsync(user, cancellationToken);

        _logger.LogInformation(
            "Generated tokens for user {UserId} (Type: {UserType}) from phone verification",
            user.Id.Value,
            user.Type);

        return new RegisterFromVerifiedPhoneResult(
            user.Id.Value,
            phoneNumber.Value,
            accessToken,
            refreshToken.Token,
            86400, // 24 hours in seconds
            isNewUser ? "Account created successfully" : "Logged in successfully");
    }
}
