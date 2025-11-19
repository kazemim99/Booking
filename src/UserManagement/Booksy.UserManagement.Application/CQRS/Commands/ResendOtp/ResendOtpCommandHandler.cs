// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/ResendOtp/ResendOtpCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.ResendOtp
{
    /// <summary>
    /// Handler for ResendOtpCommand
    /// </summary>
    public sealed class ResendOtpCommandHandler : ICommandHandler<ResendOtpCommand, ResendOtpResult>
    {
        private readonly IPhoneVerificationRepository _repository;
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<ResendOtpCommandHandler> _logger;

        public ResendOtpCommandHandler(
            IPhoneVerificationRepository repository,
            ISmsNotificationService smsService,
            ILogger<ResendOtpCommandHandler> logger)
        {
            _repository = repository;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<ResendOtpResult> Handle(
            ResendOtpCommand command,
            CancellationToken cancellationToken)
        {
            // Get verification
            var verificationId = VerificationId.From(command.VerificationId);
            var verification = await _repository.GetByIdAsync(verificationId, cancellationToken);

            if (verification == null)
            {
                throw new NotFoundException($"Verification with ID {command.VerificationId} not found");
            }
            var message = string.Empty;
            // Check if resend is allowed
            if (!verification.CanResend())
            {
                message = verification.IsExpired()
                   ? "Verification has expired. Please request a new verification."
                   : verification.IsBlocked()
                       ? $"Too many failed attempts. Please try again after {verification.BlockedUntil:HH:mm}"
                       : verification.SendAttempts >= 3
                           ? "Maximum resend attempts reached. Please request a new verification."
                           : verification.LastSentAt.HasValue && (DateTime.UtcNow - verification.LastSentAt.Value).TotalSeconds < 60
                               ? $"Please wait {60 - (int)(DateTime.UtcNow - verification.LastSentAt.Value).TotalSeconds} seconds before resending."
                               : "Cannot resend OTP at this time.";

                return new ResendOtpResult(
                    false,
                    message,
                    verification.PhoneNumber.ToNational(),
                    CanResendAfter: verification.LastSentAt?.AddSeconds(60));
            }

            // Update metadata if provided
            if (command.IpAddress != null || command.UserAgent != null || command.SessionId != null)
            {
                verification.SetMetadata(command.IpAddress, command.UserAgent, command.SessionId);
            }

            // Generate new OTP and reset state
            verification.Resend();

            // Send new OTP via SMS
            var otpCode = verification.OtpCode.Value;
            message = $"Your Booksy verification code is: {otpCode}. Valid for 5 minutes. Do not share this code.";

            var smsResult = await _smsService.SendSmsAsync(
                verification.PhoneNumber.ToNational(),
                message,
                new Dictionary<string, object>
                {
                    ["VerificationId"] = verification.Id.Value.ToString(),
                    ["ResendAttempt"] = verification.SendAttempts.ToString()
                },
                cancellationToken);

            if (!smsResult.Success)
            {
                _logger.LogError(
                    "Failed to resend OTP SMS. VerificationId: {VerificationId}, Error: {Error}",
                    verification.Id.Value,
                    smsResult.ErrorMessage);

                throw new ExternalServiceException(
                    $"Failed to resend verification code: {smsResult.ErrorMessage}",
                    "SMS_SERVICE_ERROR");
            }

            verification.MarkAsSent();
            await _repository.UpdateAsync(verification, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var remainingResends = Math.Max(0, 3 - verification.SendAttempts);

            _logger.LogInformation(
                "OTP resent successfully. VerificationId: {VerificationId}, Phone: {Phone}, SendAttempt: {SendAttempt}",
                verification.Id.Value,
                verification.PhoneNumber.Value,
                verification.SendAttempts);

            return new ResendOtpResult(
                true,
                "Verification code resent successfully",
                verification.PhoneNumber.ToNational(),
                verification.ExpiresAt,
                remainingResends > 0 ? remainingResends : null);
        }
    }
}
