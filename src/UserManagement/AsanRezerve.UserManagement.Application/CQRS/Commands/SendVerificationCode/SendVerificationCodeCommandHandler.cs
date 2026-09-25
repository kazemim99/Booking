// ========================================
// SendVerificationCodeCommandHandler.cs
// Handles sending OTP verification codes via SMS
// ========================================
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Configuration;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.SendVerificationCode;

/// <summary>
/// Handler for SendVerificationCode command
/// Sends OTP code to phone number via SMS
/// </summary>
public sealed class SendVerificationCodeCommandHandler
    : ICommandHandler<SendVerificationCodeCommand, SendVerificationCodeResponse>
{
    private readonly IPhoneVerificationRepository _repository;
    private readonly ISmsNotificationService _smsService;
    private readonly OtpProtectionOptions _protection;
    private readonly ILogger<SendVerificationCodeCommandHandler> _logger;

    public SendVerificationCodeCommandHandler(
        IPhoneVerificationRepository repository,
        ISmsNotificationService smsService,
        IOptions<OtpProtectionOptions> protection,
        ILogger<SendVerificationCodeCommandHandler> logger)
    {
        _repository = repository;
        _smsService = smsService;
        _protection = protection.Value;
        _logger = logger;
    }

    public async Task<SendVerificationCodeResponse> Handle(
        SendVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        // PhoneNumber.From canonicalizes every accepted input format (local
        // "09…", "+98…", "0098…", bare national) to one E.164 value, so the
        // country code must NOT be concatenated here — doing so produced the
        // invalid "+9809121234567" that no later lookup could match.
        PhoneNumber phoneNumber;
        try
        {
            phoneNumber = PhoneNumber.From(request.PhoneNumber);
        }
        catch (ArgumentException ex)
        {
            throw new DomainValidationException("PhoneNumber", $"Invalid phone number: {ex.Message}");
        }

        // Abuse protection for the anonymous send path. Two rules, both per PHONE, because that is
        // what an SMS costs money against — a limiter keyed on IP cannot see the number.
        //
        // This block used to sit inside `#if !DEBUG`, so the only protection on the endpoint
        // disappeared in any Debug build: whether a phone could be bombed depended on how the binary
        // was compiled. The limits are configuration now (OtpProtectionOptions), so a test host
        // raises them deliberately and a deployed host cannot lose them by accident.
        var recentVerifications = await _repository.GetRecentVerificationsByPhoneAsync(
            phoneNumber.Value,
            _protection.Window,
            cancellationToken);

        if (recentVerifications.Count >= _protection.MaxSendsPerWindow)
        {
            var oldest = recentVerifications.Min(v => v.CreatedAt);
            var retryAfter = _protection.Window - (DateTime.UtcNow - oldest);

            _logger.LogWarning(
                "OTP send refused: {Count} codes already sent to {MaskedPhone} within {Window}",
                recentVerifications.Count, MaskPhoneNumber(phoneNumber.Value), _protection.Window);

            throw new TooManyRequestsException(
                "تعداد درخواست بیش از حد. لطفا دقایقی دیگر مجدد تلاش کنید",
                retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromMinutes(1));
        }

        // The cooldown PhoneVerification.CanResend() has always modelled, applied to the path that
        // creates a fresh verification each time and therefore never consulted it. It depends on
        // stored timestamps reading back as the UTC instants they are — which they did not until
        // FOLLOW-UPS #48 removed Npgsql's legacy timestamp behaviour.
        var lastSentAt = recentVerifications
            .Select(v => v.LastSentAt ?? v.CreatedAt)
            .DefaultIfEmpty()
            .Max();

        var sinceLastSend = DateTime.UtcNow - lastSentAt;

        if (lastSentAt != default && sinceLastSend < _protection.ResendCooldown)
        {
            var wait = _protection.ResendCooldown - sinceLastSend;

            _logger.LogWarning(
                "OTP send refused by cooldown for {MaskedPhone}: last sent {LastSentAt:o}, {Elapsed} ago, cooldown {Cooldown}",
                MaskPhoneNumber(phoneNumber.Value), lastSentAt, sinceLastSend, _protection.ResendCooldown);

            throw new TooManyRequestsException(
                "کد قبلا ارسال شده است. لطفا کمی صبر کنید",
                wait > TimeSpan.Zero ? wait : TimeSpan.FromSeconds(1));
        }

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
        var smsMessage = $"Your AsanRezerve verification code is: {verification.OtpCode.Value}\nValid for 10 minutes.";
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
