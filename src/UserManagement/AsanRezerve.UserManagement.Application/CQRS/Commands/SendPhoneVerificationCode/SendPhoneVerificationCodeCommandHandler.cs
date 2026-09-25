// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/SendPhoneVerificationCode/SendPhoneVerificationCodeCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.SendPhoneVerificationCode;

/// <summary>
/// Handler for sending a phone-CHANGE verification code to an authenticated user's profile
/// (the Vue provider dashboard's "PhoneVerificationModal" — a real, live UI feature).
/// </summary>
/// <remarks>
/// This handler used to be entirely commented out (no implementation at all), which made this
/// class's own live, [Authorize]-protected endpoint (<c>POST /users/{id}/phone/send-verification</c>)
/// throw "handler not found" on every call. Rather than restore the old ad-hoc
/// <c>IDistributedCache</c>-based design it once had (which never actually sent an SMS, and never
/// persisted its "code" anywhere durable), this rewrite goes through the same canonical
/// <see cref="Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification"/> aggregate the
/// sign-in OTP flow uses — <see cref="Enums.VerificationPurpose.PhoneNumberChange"/> already
/// existed in that enum for exactly this purpose and had no caller anywhere in the codebase.
/// </remarks>
public sealed class SendPhoneVerificationCodeCommandHandler
    : IRequestHandler<SendPhoneVerificationCodeCommand, SendPhoneVerificationCodeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationRepository _verificationRepository;
    private readonly ISmsNotificationService _smsService;
    private readonly ILogger<SendPhoneVerificationCodeCommandHandler> _logger;

    public SendPhoneVerificationCodeCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationRepository verificationRepository,
        ISmsNotificationService smsService,
        ILogger<SendPhoneVerificationCodeCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationRepository = verificationRepository ?? throw new ArgumentNullException(nameof(verificationRepository));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendPhoneVerificationCodeResult> Handle(
        SendPhoneVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId.ToString());
        }

        PhoneNumber phoneNumber;
        try
        {
            phoneNumber = PhoneNumber.From(request.PhoneNumber);
        }
        catch (ArgumentException ex)
        {
            throw new DomainValidationException("PhoneNumber", $"Invalid phone number: {ex.Message}");
        }

        // Re-verifying the account's own current number must be allowed; taking over a
        // DIFFERENT person's number must not be -- that is the "one person per phone"
        // invariant this whole identity model exists to protect (see [[refactor-identity-and-membership]]).
        var ownerOfNumber = await _userRepository.GetByPhoneNumberAsync(phoneNumber.Value, cancellationToken);
        if (ownerOfNumber != null && ownerOfNumber.Id != userId)
        {
            throw new DomainValidationException(
                "PhoneNumber",
                "این شماره موبایل قبلاً توسط کاربر دیگری ثبت شده است");
        }

        // Rate limiting, mirroring SendVerificationCodeCommandHandler's own gate. Left out of
        // Debug builds for the same reason that handler leaves it out: it would make the
        // integration/unit test suite (which runs Debug) flaky under repeated runs.
#if !DEBUG
        var recentVerifications = await _verificationRepository.GetRecentVerificationsByPhoneAsync(
            phoneNumber.Value,
            TimeSpan.FromMinutes(10),
            cancellationToken);

        if (recentVerifications.Count >= 3)
        {
            throw new DomainValidationException(
                "PhoneNumber",
                "تعداد درخواست بیش از حد. لطفا دقایقی دیگر مجدد تلاش کنید");
        }
#endif

        var verification = Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification.Create(
            phoneNumber,
            VerificationMethod.Sms,
            VerificationPurpose.PhoneNumberChange,
            userId);

        await _verificationRepository.AddAsync(verification, cancellationToken);
        await _verificationRepository.SaveChangesAsync(cancellationToken);

        var smsMessage = $"کد تأیید بوکسی: {verification.OtpCode.Value}\nاعتبار: 5 دقیقه";
        var (success, messageId, errorMessage) = await _smsService.SendSmsAsync(
            phoneNumber.Value,
            smsMessage,
            metadata: new Dictionary<string, object>
            {
                ["VerificationId"] = verification.Id.Value,
                ["Purpose"] = nameof(VerificationPurpose.PhoneNumberChange),
            },
            cancellationToken);

        if (!success)
        {
            _logger.LogError(
                "Failed to send phone-change verification code. VerificationId: {VerificationId}, Error: {Error}",
                verification.Id.Value,
                errorMessage);

            throw new InvalidOperationException("Failed to send verification code. Please try again later.");
        }

        _logger.LogInformation(
            "Phone-change verification code sent. VerificationId: {VerificationId}, MessageId: {MessageId}, UserId: {UserId}",
            verification.Id.Value,
            messageId,
            request.UserId);

        return new SendPhoneVerificationCodeResult(
            Success: true,
            Message: "کد تأیید ارسال شد",
            ExpiresAt: verification.ExpiresAt);
    }
}
