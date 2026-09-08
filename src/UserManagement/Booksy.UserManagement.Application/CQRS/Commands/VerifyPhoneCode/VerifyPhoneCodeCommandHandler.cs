// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/VerifyPhoneCode/VerifyPhoneCodeCommandHandler.cs
// ========================================
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.VerifyPhoneCode;

/// <summary>
/// Handler for verifying a phone-CHANGE code and applying it to the user's account. Completes
/// the flow started by <see cref="SendPhoneVerificationCode.SendPhoneVerificationCodeCommandHandler"/>.
/// </summary>
/// <remarks>
/// Was entirely commented out (see that handler's remarks for why); rewritten onto the same
/// canonical <see cref="Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification"/> aggregate
/// rather than resurrecting the old <c>IDistributedCache</c> design.
/// </remarks>
public sealed class VerifyPhoneCodeCommandHandler
    : IRequestHandler<VerifyPhoneCodeCommand, VerifyPhoneCodeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationRepository _verificationRepository;
    private readonly ILogger<VerifyPhoneCodeCommandHandler> _logger;

    public VerifyPhoneCodeCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationRepository verificationRepository,
        ILogger<VerifyPhoneCodeCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationRepository = verificationRepository ?? throw new ArgumentNullException(nameof(verificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerifyPhoneCodeResult> Handle(
        VerifyPhoneCodeCommand request,
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

        var verification = await _verificationRepository.GetByPhoneAndPurposeAsync(
            phoneNumber.Value, VerificationPurpose.PhoneNumberChange, cancellationToken);

        if (verification == null)
        {
            _logger.LogWarning("No phone-change verification found for {UserId}", request.UserId);
            return new VerifyPhoneCodeResult(false, "کد تأیید منقضی شده یا نامعتبر است");
        }

        // A replayed already-verified code: PhoneVerification.Verify() would throw for this
        // (it treats re-verifying as a caller bug, not a user-facing outcome), so it is checked
        // here and turned into the same friendly "expired/invalid" result as every other
        // not-verifiable state, rather than letting the exception surface as a 500.
        if (verification.Status == VerificationStatus.Verified)
        {
            return new VerifyPhoneCodeResult(false, "این کد قبلاً استفاده شده است. کد جدید درخواست کنید");
        }

        if (verification.IsBlocked())
        {
            return new VerifyPhoneCodeResult(false, "تعداد تلاش‌های ناموفق بیش از حد مجاز. لطفا بعدا تلاش کنید");
        }

        if (verification.IsExpired())
        {
            return new VerifyPhoneCodeResult(false, "کد تأیید منقضی شده است");
        }

        var isValid = verification.Verify(request.VerificationCode);

        // Whatever Verify() just mutated (attempt count, Status, possibly BlockedUntil) must be
        // persisted regardless of outcome -- a failed attempt still needs to count toward the
        // lockout, exactly like the canonical sign-in VerifyPhoneCommandHandler.
        await _verificationRepository.UpdateAsync(verification, cancellationToken);

        if (!isValid)
        {
            var remaining = verification.RemainingAttempts();
            var message = remaining > 0
                ? $"کد تأیید نامعتبر است. {remaining} تلاش باقی مانده"
                : "تعداد تلاش‌های ناموفق بیش از حد مجاز. حساب شما موقتا مسدود شد";

            await _verificationRepository.SaveChangesAsync(cancellationToken);
            return new VerifyPhoneCodeResult(false, message);
        }

        // Re-check uniqueness right before committing: the number could have been claimed by
        // someone else in the window between send and verify. The OTP itself is already spent
        // either way (Verify() marked it Verified above) -- a caller hitting this would need to
        // request a fresh code for a different number.
        var ownerOfNumber = await _userRepository.GetByPhoneNumberAsync(phoneNumber.Value, cancellationToken);
        if (ownerOfNumber != null && ownerOfNumber.Id != userId)
        {
            await _verificationRepository.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "Phone {Phone} was claimed by another user between send and verify for {UserId}",
                phoneNumber.Value, request.UserId);
            return new VerifyPhoneCodeResult(false, "این شماره موبایل قبلاً توسط کاربر دیگری ثبت شده است");
        }

        user.SetPhoneNumber(phoneNumber);
        user.VerifyPhoneNumber();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Single explicit save: User and PhoneVerification share the same UserManagementDbContext
        // instance within this request scope, so one SaveChangesAsync call (via either
        // repository) persists both tracked changes together.
        await _verificationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Phone number changed and verified for UserId: {UserId}", request.UserId);

        return new VerifyPhoneCodeResult(
            Success: true,
            Message: "شماره موبایل با موفقیت تأیید شد",
            PhoneNumber: phoneNumber.ToNational(),
            VerifiedAt: verification.VerifiedAt);
    }
}
