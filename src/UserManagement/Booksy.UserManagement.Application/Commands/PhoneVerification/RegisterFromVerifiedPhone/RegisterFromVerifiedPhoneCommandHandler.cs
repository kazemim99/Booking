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
using Booksy.UserManagement.Application.Services;
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
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<RegisterFromVerifiedPhoneCommandHandler> _logger;

    public RegisterFromVerifiedPhoneCommandHandler(
        IPhoneVerificationRepository verificationRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator tokenGenerator,
        ILogger<RegisterFromVerifiedPhoneCommandHandler> logger)
    {
        _verificationRepository = verificationRepository;
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
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

        // 4. Generate JWT tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(
            user.Id.Value,
            user.Email.Value,
            user.Roles.Select(r => r.Name).ToList());

        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Store refresh token
        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
        await _userRepository.SaveAsync(user, cancellationToken);

        _logger.LogInformation(
            "Generated tokens for user {UserId} from phone verification",
            user.Id.Value);

        return new RegisterFromVerifiedPhoneResult(
            user.Id.Value,
            phoneNumber.Value,
            accessToken,
            refreshToken,
            3600, // 1 hour expiry
            isNewUser ? "Account created successfully" : "Logged in successfully");
    }
}
