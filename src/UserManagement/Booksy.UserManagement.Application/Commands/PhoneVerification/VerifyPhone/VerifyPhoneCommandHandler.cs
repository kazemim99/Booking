// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.VerifyPhone
{
    /// <summary>
    /// Handler for VerifyPhoneCommand
    /// </summary>
    public sealed class VerifyPhoneCommandHandler : ICommandHandler<VerifyPhoneCommand, VerifyPhoneResult>
    {
        private readonly IPhoneVerificationRepository _repository;
        private readonly ILogger<VerifyPhoneCommandHandler> _logger;

        public VerifyPhoneCommandHandler(
            IPhoneVerificationRepository repository,
            ILogger<VerifyPhoneCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<VerifyPhoneResult> Handle(
            VerifyPhoneCommand command,
            CancellationToken cancellationToken)
        {
            // Get verification
            var verificationId = VerificationId.From(command.VerificationId);
            var verification = await _repository.GetByIdAsync(verificationId, cancellationToken);

            if (verification == null)
            {
                throw new NotFoundException($"Verification with ID {command.VerificationId} not found");
            }

            // Check if blocked
            if (verification.IsBlocked())
            {
                return new VerifyPhoneResult(
                    false,
                    $"Too many failed attempts. Please try again after {verification.BlockedUntil:HH:mm}",
                    verification.PhoneNumber.ToNational(),
                    BlockedUntil: verification.BlockedUntil);
            }

            // Check if expired
            if (verification.IsExpired())
            {
                return new VerifyPhoneResult(
                    false,
                    "Verification code has expired. Please request a new code.",
                    verification.PhoneNumber.ToNational());
            }

            // Verify the code
            bool success = verification.Verify(command.Code);

            // Save changes
            await _repository.UpdateAsync(verification, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            if (success)
            {
                _logger.LogInformation(
                    "Phone verified successfully. VerificationId: {VerificationId}, Phone: {Phone}",
                    verification.Id.Value,
                    verification.PhoneNumber.Value);

                return new VerifyPhoneResult(
                    true,
                    "Phone number verified successfully",
                    verification.PhoneNumber.ToNational(),
                    VerifiedAt: verification.VerifiedAt);
            }
            else
            {
                var remainingAttempts = verification.RemainingAttempts();

                _logger.LogWarning(
                    "Phone verification failed. VerificationId: {VerificationId}, RemainingAttempts: {RemainingAttempts}",
                    verification.Id.Value,
                    remainingAttempts);

                var message = remainingAttempts > 0
                    ? $"Invalid verification code. {remainingAttempts} attempts remaining."
                    : "Maximum attempts reached. Your verification has been blocked for 1 hour.";

                return new VerifyPhoneResult(
                    false,
                    message,
                    verification.PhoneNumber.ToNational(),
                    RemainingAttempts: remainingAttempts > 0 ? remainingAttempts : null,
                    BlockedUntil: verification.BlockedUntil);
            }
        }
    }
}
