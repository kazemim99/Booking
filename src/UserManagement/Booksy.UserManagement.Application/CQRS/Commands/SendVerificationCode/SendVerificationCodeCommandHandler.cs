// ========================================
// SendVerificationCodeCommandHandler.cs
// Handles sending OTP verification codes via SMS
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.SendVerificationCode;

/// <summary>
/// Handler for SendVerificationCode command
/// Sends OTP code to phone number via SMS
/// </summary>
public sealed class SendVerificationCodeCommandHandler
    : ICommandHandler<SendVerificationCodeCommand, SendVerificationCodeResponse>
{
    private readonly IPhoneVerificationRepository _repository;
    private readonly ISmsNotificationService _smsService;
    private readonly ILogger<SendVerificationCodeCommandHandler> _logger;

    public SendVerificationCodeCommandHandler(
        IPhoneVerificationRepository repository,
        ISmsNotificationService smsService,
        ILogger<SendVerificationCodeCommandHandler> logger)
    {
        _repository = repository;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<SendVerificationCodeResponse> Handle(
        SendVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        // Normalize phone number with country code
        var phoneNumberString = request.PhoneNumber.StartsWith("+")
            ? request.PhoneNumber
            : $"{request.CountryCode}{request.PhoneNumber}";

        // Validate phone number format
        PhoneNumber phoneNumber;
        try
        {
            phoneNumber = PhoneNumber.Create(phoneNumberString);
        }
        catch (ArgumentException ex)
        {
            throw new DomainValidationException("PhoneNumber", $"Invalid phone number: {ex.Message}");
        }

        // Check for recent verification attempts (rate limiting)
        var recentVerifications = await _repository.GetRecentVerificationsByPhoneAsync(
            phoneNumber.Value,
            TimeSpan.FromMinutes(10),
            cancellationToken);

#if !DEBUG
        if (recentVerifications.Count >= 3)
        {
            throw new DomainValidationException(
                phoneNumber.Value,
                "تعداد درخواست بیش از حد. لطفا دقایقی دیگر مجدد تلاش کنید");
        }
#endif

        // Create verification aggregate
        var verification = Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification.Create(
            phoneNumber,
            VerificationMethod.Sms,
            VerificationPurpose.Registration,
            userId: null);

        // Set metadata
        verification.SetMetadata(request.IpAddress, request.UserAgent, sessionId: null);

        // Save to database
        await _repository.AddAsync(verification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Send OTP via SMS
        var smsMessage = $"Your Booksy verification code is: {verification.OtpCode.Value}\nValid for 10 minutes.";
        var (success, messageId, errorMessage) = await _smsService.SendSmsAsync(
            phoneNumber.Value,
            smsMessage,
            metadata: new Dictionary<string, object>
            {
                ["VerificationId"] = verification.Id.Value,
                ["Purpose"] = "PhoneVerification"
            },
            cancellationToken);

        if (!success)
        {
            _logger.LogError(
                "Failed to send verification code. VerificationId: {VerificationId}, Error: {Error}",
                verification.Id.Value,
                errorMessage);

            throw new InvalidOperationException(
                "Failed to send verification code. Please try again later.");
        }

        _logger.LogInformation(
            "Verification code sent successfully. VerificationId: {VerificationId}, MessageId: {MessageId}, Phone: {MaskedPhone}",
            verification.Id.Value,
            messageId,
            MaskPhoneNumber(phoneNumber.Value));

        return new SendVerificationCodeResponse(
            VerificationId: verification.Id.Value,
            MaskedPhoneNumber: MaskPhoneNumber(phoneNumber.Value),
            ExpiresAt: verification.ExpiresAt,
            MaxAttempts: 5, // Default max attempts
            Message: $"Verification code sent to {MaskPhoneNumber(phoneNumber.Value)}"
        );
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return phoneNumber;

        var lastDigits = phoneNumber[^4..];
        return $"•••{lastDigits}";
    }
}
