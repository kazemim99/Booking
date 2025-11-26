using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.VerifyPhoneCode;

/// <summary>
/// Handler for verifying phone code
/// </summary>
public class VerifyPhoneCodeCommandHandler : IRequestHandler<VerifyPhoneCodeCommand, VerifyPhoneCodeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<VerifyPhoneCodeCommandHandler> _logger;

    public VerifyPhoneCodeCommandHandler(
        IUserRepository userRepository,
        IDistributedCache cache,
        ILogger<VerifyPhoneCodeCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerifyPhoneCodeResult> Handle(
        VerifyPhoneCodeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying phone code for {PhoneNumber}, user {UserId}",
            request.PhoneNumber, request.UserId);

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId.ToString());
        }

        // Get cached verification code
        var cacheKey = $"phone_verification:{request.UserId}:{request.PhoneNumber}";
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedValue))
        {
            _logger.LogWarning("No verification code found in cache for {PhoneNumber}", request.PhoneNumber);
            return new VerifyPhoneCodeResult(
                Success: false,
                Message: "کد تأیید منقضی شده یا نامعتبر است"
            );
        }

        // Parse cached value (format: "code:expiresAt")
        var parts = cachedValue.Split(':');
        if (parts.Length < 2)
        {
            _logger.LogError("Invalid cached value format for {PhoneNumber}", request.PhoneNumber);
            return new VerifyPhoneCodeResult(
                Success: false,
                Message: "خطا در بررسی کد تأیید"
            );
        }

        var storedCode = parts[0];
        var expiresAt = DateTime.Parse(string.Join(':', parts.Skip(1)));

        // Check if code has expired
        if (DateTime.UtcNow > expiresAt)
        {
            _logger.LogWarning("Verification code expired for {PhoneNumber}", request.PhoneNumber);
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return new VerifyPhoneCodeResult(
                Success: false,
                Message: "کد تأیید منقضی شده است"
            );
        }

        // Verify code matches
        if (storedCode != request.VerificationCode)
        {
            _logger.LogWarning("Invalid verification code for {PhoneNumber}", request.PhoneNumber);
            return new VerifyPhoneCodeResult(
                Success: false,
                Message: "کد تأیید نامعتبر است"
            );
        }

        // Code is valid - update user's phone number
        var phoneNumber = PhoneNumber.From(request.PhoneNumber);
        user.SetPhoneNumber(phoneNumber);
        user.VerifyPhoneNumber();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Remove the verification code from cache
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogInformation("Phone number {PhoneNumber} verified successfully for user {UserId}",
            request.PhoneNumber, request.UserId);

        return new VerifyPhoneCodeResult(
            Success: true,
            Message: "شماره موبایل با موفقیت تأیید شد",
            PhoneNumber: request.PhoneNumber,
            VerifiedAt: DateTime.UtcNow
        );
    }
}
