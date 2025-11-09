// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/RequestVerification/RequestPhoneVerificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.RequestVerification
{
    /// <summary>
    /// Handler for RequestPhoneVerificationCommand
    /// </summary>
    public sealed class RequestPhoneVerificationCommandHandler
        : ICommandHandler<RequestPhoneVerificationCommand, RequestPhoneVerificationResult>
    {
        private readonly IPhoneVerificationRepository _repository;
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<RequestPhoneVerificationCommandHandler> _logger;

        public RequestPhoneVerificationCommandHandler(
            IPhoneVerificationRepository repository,
            ISmsNotificationService smsService,
            ILogger<RequestPhoneVerificationCommandHandler> logger)
        {
            _repository = repository;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<RequestPhoneVerificationResult> Handle(
            RequestPhoneVerificationCommand command,
            CancellationToken cancellationToken)
        {
            // Validate phone number format
            PhoneNumber phoneNumber;
            try
            {
                phoneNumber = PhoneNumber.Create(command.PhoneNumber);
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

            if (recentVerifications.Count >= 3)
            {
                throw new DomainValidationException(
                    $"{phoneNumber}",
                    "تعداد درخواست بیش از حد.لطفا دقایقی دیگر مجدد تلاش کنید");
            }

            // Create verification aggregate
            UserId? userId = command.UserId.HasValue ? UserId.From(command.UserId.Value) : null;

            var verification = Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification.Create(
                phoneNumber,
                command.Method,
                command.Purpose,
                userId);

            // Set metadata
            verification.SetMetadata(command.IpAddress, command.UserAgent, command.SessionId);

            // Save to database
            await _repository.AddAsync(verification, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // Send OTP via SMS
            var otpCode = verification.OtpCode.Value;
            var message = $"Your Booksy verification code is: {otpCode}. Valid for 5 minutes. Do not share this code.";

            var smsResult = await _smsService.SendSmsAsync(
                phoneNumber.ToNational(),
                message,
                new Dictionary<string, object>
                {
                    ["VerificationId"] = verification.Id.Value.ToString(),
                    ["Purpose"] = command.Purpose.ToString()
                },
                cancellationToken);

            if (!smsResult.Success)
            {
                _logger.LogError(
                    "Failed to send OTP SMS. VerificationId: {VerificationId}, Error: {Error}",
                    verification.Id.Value,
                    smsResult.ErrorMessage);

                throw new ExternalServiceException(
                    $"Failed to send verification code: {smsResult.ErrorMessage}",
                    "SMS_SERVICE_ERROR");
            }

            verification.MarkAsSent();
            await _repository.UpdateAsync(verification, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Phone verification OTP sent successfully. VerificationId: {VerificationId}, Phone: {Phone}",
                verification.Id.Value,
                phoneNumber.Value);

            return new RequestPhoneVerificationResult(
                verification.Id.Value,
                phoneNumber.ToNational(),
                verification.Method,
                verification.ExpiresAt,
                verification.MaxVerificationAttempts,
                "Verification code sent successfully");
        }
    }
}
