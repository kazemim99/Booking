using Booksy.Infrastructure.External.Notifications;
using Booksy.Infrastructure.External.OTP;
using Booksy.Infrastructure.External.sms;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Booksy.UserManagement.Infrastructure.Services.Application;

public class PhoneVerificationService : IPhoneVerificationService
{
    private readonly IPhoneVerificationRepository _verificationRepository;
    private readonly IOtpService _otpService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhoneVerificationService> _logger;
    private readonly IRahyabSmsSender _smsService;

    private readonly int _codeExpirationMinutes;
    private readonly List<string> _testNumbers;
    private readonly bool _sandboxMode;

    public PhoneVerificationService(
        IPhoneVerificationRepository verificationRepository,
        IOtpService otpService,
        IConfiguration configuration,
        ILogger<PhoneVerificationService> logger,
        IRahyabSmsSender smsService)
    {
        _verificationRepository = verificationRepository;
        _otpService = otpService;
        _configuration = configuration;
        _logger = logger;
        _smsService = smsService;

        _codeExpirationMinutes = int.Parse(_configuration["PhoneVerification:ExpirationMinutes"] ?? "5");
        _testNumbers = _configuration.GetSection("PhoneVerification:TestNumbers").Get<List<string>>() ?? new List<string> { "2222" };
        _sandboxMode = bool.Parse(_configuration["PhoneVerification:SandboxMode"] ?? "false");
    }

    public async Task<(PhoneVerification Verification, string MaskedPhone, int ExpiresInSeconds)> SendVerificationCodeAsync(
        string phoneNumber,
        string countryCode,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        phoneNumber = NormalizePhoneNumber(phoneNumber);


        // Check for existing active verification
        var existingVerification = await _verificationRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (existingVerification != null && !existingVerification.IsExpired() && !existingVerification.IsVerified)
        {
            // Delete old verification to allow new one
            await _verificationRepository.DeleteAsync(existingVerification, cancellationToken);
            _logger.LogInformation("Invalidated existing verification for phone: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
        }


        var code = _otpService.GetCode(phoneNumber);
        _logger.LogInformation("Generated OTP code for phone: {MaskedPhone}", MaskPhoneNumber(phoneNumber));

        // Hash the code before storing
        string hashedCode = HashCode(code);

        // Create verification entity
        var verification = new PhoneVerification(
            Guid.NewGuid(),
            phoneNumber,
            countryCode,
            hashedCode,
            DateTime.UtcNow.AddMinutes(_codeExpirationMinutes)
        );

        verification.SetRequestInfo(ipAddress, userAgent);

        // Save to database
        await _verificationRepository.AddAsync(verification, cancellationToken);

        await _smsService.SendAsync(phoneNumber, code);


        return (verification, MaskPhoneNumber(phoneNumber), _codeExpirationMinutes * 60);
    }

    public async Task<(bool IsValid, int RemainingAttempts, string? ErrorMessage)> VerifyCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        // Normalize phone number
        phoneNumber = NormalizePhoneNumber(phoneNumber);

        // Get latest verification
        var verification = await _verificationRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (verification == null)
        {
            _logger.LogWarning("No verification found for phone: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
            return (false, 0, "No verification request found. Please request a new code.");
        }

        // Check if expired
        if (verification.IsExpired())
        {
            _logger.LogWarning("Verification expired for phone: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
            return (false, 0, "Verification code has expired. Please request a new code.");
        }

        // Check if already verified
        if (verification.IsVerified)
        {
            _logger.LogWarning("Phone already verified: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
            return (false, 0, "Phone number is already verified.");
        }

        // Check max attempts
        if (verification.IsMaxAttemptsReached())
        {
            _logger.LogWarning("Max attempts reached for phone: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
            return (false, 0, "Maximum verification attempts reached. Please request a new code.");
        }

        // Increment attempt count
        verification.IncrementAttemptCount();
        await _verificationRepository.UpdateAsync(verification, cancellationToken);

        // Verify code
        bool isValid;

        // Real number: verify against hashed code
        string hashedInput = HashCode(code);
        isValid = string.Equals(verification.HashedCode, hashedInput, StringComparison.Ordinal);

        if (isValid)
        {
            verification.MarkAsVerified();
            await _verificationRepository.UpdateAsync(verification, cancellationToken);
            _logger.LogInformation("Phone successfully verified: {MaskedPhone}", MaskPhoneNumber(phoneNumber));
            return (true, verification.GetRemainingAttempts(), null);
        }
        else
        {
            _logger.LogWarning("Invalid code for phone: {MaskedPhone}. Attempts: {AttemptCount}/{MaxAttempts}",
                MaskPhoneNumber(phoneNumber), verification.AttemptCount, verification.MaxAttempts);

            int remaining = verification.GetRemainingAttempts();
            string errorMessage = remaining > 0
                ? $"Invalid code. {remaining} attempt{(remaining > 1 ? "s" : "")} remaining."
                : "Maximum attempts reached. Please request a new code.";

            return (false, remaining, errorMessage);
        }
    }



    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return phoneNumber;

        // Example: +4917012345678 → +49 170 ••• ••78
        if (phoneNumber.StartsWith("+"))
        {
            int length = phoneNumber.Length;
            string last2 = phoneNumber.Substring(length - 2);
            string first = phoneNumber.Substring(0, Math.Min(7, length - 2));

            return $"{first} ••• ••{last2}";
        }

        // Fallback: mask middle digits
        int halfLength = phoneNumber.Length / 2;
        return phoneNumber.Substring(0, 2) + new string('•', halfLength) + phoneNumber.Substring(phoneNumber.Length - 2);
    }

    public async Task<int> CleanupExpiredVerificationsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1); // Delete verifications older than 1 hour
        var deletedCount = await _verificationRepository.DeleteExpiredAsync(cutoffTime, cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired phone verifications", deletedCount);
        }

        return deletedCount;
    }



    private string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove spaces, dashes, parentheses
        return Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");
    }

    private string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

