using Booksy.Core.Domain.Exceptions;
using Booksy.UserManagement.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Booksy.UserManagement.Application.CQRS.Commands.SendPhoneVerificationCode;

/// <summary>
/// Handler for sending phone verification code
/// </summary>
public class SendPhoneVerificationCodeCommandHandler : IRequestHandler<SendPhoneVerificationCodeCommand, SendPhoneVerificationCodeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SendPhoneVerificationCodeCommandHandler> _logger;
    // Note: In production, inject ISmsService to actually send SMS

    public SendPhoneVerificationCodeCommandHandler(
        IUserRepository userRepository,
        IDistributedCache cache,
        ILogger<SendPhoneVerificationCodeCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendPhoneVerificationCodeResult> Handle(
        SendPhoneVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending phone verification code to {PhoneNumber} for user {UserId}",
            request.PhoneNumber, request.UserId);

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId.ToString());
        }

        // Validate phone number format (Iranian phone number)
        if (!IsValidIranianPhoneNumber(request.PhoneNumber))
        {
            throw new ValidationException("شماره موبایل باید با 09 شروع شود و 11 رقم باشد");
        }

        // Check if phone number is already verified by another user
        var existingUser = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (existingUser != null && existingUser.Id != request.UserId)
        {
            throw new ValidationException("این شماره موبایل قبلاً ثبت شده است");
        }

        // Generate 6-digit verification code
        var verificationCode = GenerateVerificationCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(5); // Code expires in 5 minutes

        // Store code in cache with user ID and phone number
        var cacheKey = $"phone_verification:{request.UserId}:{request.PhoneNumber}";
        var cacheValue = $"{verificationCode}:{expiresAt:O}";

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };

        await _cache.SetStringAsync(cacheKey, cacheValue, cacheOptions, cancellationToken);

        // TODO: In production, integrate with SMS service
        // await _smsService.SendSmsAsync(request.PhoneNumber,
        //     $"کد تأیید بوکسی: {verificationCode}\nاعتبار: 5 دقیقه",
        //     cancellationToken);

        _logger.LogInformation("Verification code generated and cached for {PhoneNumber}. Code: {Code} (DO NOT LOG IN PRODUCTION)",
            request.PhoneNumber, verificationCode);

        return new SendPhoneVerificationCodeResult(
            Success: true,
            Message: "کد تأیید ارسال شد",
            ExpiresAt: expiresAt
        );
    }

    private static string GenerateVerificationCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static bool IsValidIranianPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove all non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Must be exactly 11 digits and start with 09
        return digitsOnly.Length == 11 && digitsOnly.StartsWith("09");
    }
}
