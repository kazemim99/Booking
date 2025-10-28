using Booksy.Core.Application.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

public class VerifyCodeHandler : ICommandHandler<VerifyCodeCommand, VerifyCodeResponse>
{
    private readonly IPhoneVerificationService _verificationService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProviderInfoService _providerInfoService;
    private readonly ILogger<VerifyCodeHandler> _logger;

    public VerifyCodeHandler(
        IPhoneVerificationService verificationService,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IProviderInfoService providerInfoService,
        ILogger<VerifyCodeHandler> logger)
    {
        _verificationService = verificationService;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _providerInfoService = providerInfoService;
        _logger = logger;
    }

    public async Task<VerifyCodeResponse> Handle(
        VerifyCodeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Verifying code for phone: {PhoneNumber}",
            _verificationService.MaskPhoneNumber(request.PhoneNumber)
        );

        // Verify the code
        var (isValid, remainingAttempts, errorMessage) = await _verificationService.VerifyCodeAsync(
            request.PhoneNumber,
            request.Code,
            cancellationToken
        );

        if (!isValid)
        {
            _logger.LogWarning(
                "Code verification failed for phone: {PhoneNumber}. Remaining attempts: {Remaining}",
                _verificationService.MaskPhoneNumber(request.PhoneNumber),
                remainingAttempts
            );

            return new VerifyCodeResponse(
                Success: false,
                AccessToken: null,
                RefreshToken: null,
                ExpiresIn: null,
                User: null,
                ErrorMessage: errorMessage,
                RemainingAttempts: remainingAttempts
            );
        }

        // Code is valid - get or create user
        var user = await GetOrCreateUserAsync(request.PhoneNumber, request.UserType, cancellationToken);

        // Query provider information if user has Provider role
        string? providerId = null;
        string? providerStatus = null;
        if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
        {
            _logger.LogInformation("User has Provider role, querying provider info for UserId: {UserId}", user.Id);
            var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
                user.Id.Value,
                cancellationToken);

            if (providerInfo != null)
            {
                providerId = providerInfo.ProviderId.ToString();
                providerStatus = providerInfo.Status;
                _logger.LogInformation("Provider found: ProviderId={ProviderId}, Status={Status}",
                    providerId, providerInfo.Status);
            }
            else
            {
                _logger.LogInformation("No provider found for UserId: {UserId}", user.Id);
            }
        }

        // Generate JWT tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(
            userId: user.Id,
            userType: user.Type,
            email: user.Email,
            displayName: $"{user.Profile.FirstName} {user.Profile.LastName}",
            user.Status.ToString(),
            roles: user.Roles.Select(r => r.Name).ToList(),
            providerId: providerId,
            providerStatus: providerStatus,
            expirationHours: 24
        );

        // TODO: Generate refresh token (implement refresh token logic separately)
        string? refreshToken = null;

        _logger.LogInformation(
            "Code verified successfully for phone: {PhoneNumber}. User ID: {UserId}",
            _verificationService.MaskPhoneNumber(request.PhoneNumber),
            user.Id.Value
        );

        return new VerifyCodeResponse(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 3600, // 1 hour
            User: new UserInfo(
                Id: user.Id.Value,
                PhoneNumber: request.PhoneNumber,
                PhoneVerified: true,
                UserType: request.UserType,
                Status : user.Status.ToString(),
                Roles: user.Roles.Select(r => r.Name).ToArray()
            ),
            ErrorMessage: null,
            RemainingAttempts: null
        );
    }

    private async Task<User> GetOrCreateUserAsync(
        string phoneNumber,
        string userType,
        CancellationToken cancellationToken)
    {
        // Try to find existing user by phone number
        var existingUser = await _userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (existingUser != null)
        {
            // Update phone verification status
            if (!existingUser.PhoneNumberVerified)
            {
                existingUser.VerifyPhoneNumber();
                await _userRepository.UpdateAsync(existingUser, cancellationToken);
            }

            return existingUser;
        }

        // Create new user
        var newUser = User.CreateFromPhoneVerification(
            id:  UserId.From(Guid.NewGuid()),
            phoneNumber:  PhoneNumber.Create(phoneNumber),
            userType: userType == "Provider" ? UserType.Provider : UserType.Customer,
            firstName: "User",
            lastName: phoneNumber.Substring(phoneNumber.Length - 4) // Temp: use last 4 digits
        );

        // Add Provider role
        newUser.AddRole("Provider");

        await _userRepository.SaveAsync(newUser, cancellationToken);

        _logger.LogInformation("Created new user from phone verification. User ID: {UserId}", newUser.Id.Value);

        return newUser;
    }
}
