using Booksy.Core.Application.CQRS;
using Booksy.UserManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;

public class SendVerificationCodeHandler : ICommandHandler<SendVerificationCodeCommand, SendVerificationCodeResponse>
{
    private readonly IPhoneVerificationService _verificationService;
    private readonly ILogger<SendVerificationCodeHandler> _logger;

    public SendVerificationCodeHandler(
        IPhoneVerificationService verificationService,
        ILogger<SendVerificationCodeHandler> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    public async Task<SendVerificationCodeResponse> Handle(
        SendVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
      
            _logger.LogInformation(
                "Sending verification code to phone: {PhoneNumber}, Country: {CountryCode}",
                _verificationService.MaskPhoneNumber(request.PhoneNumber),
                request.CountryCode
            );

            var (verification, maskedPhone, expiresInSeconds) = await _verificationService.SendVerificationCodeAsync(
                request.PhoneNumber,
                request.CountryCode,
                request.IpAddress,
                request.UserAgent,
                cancellationToken
            );

            return new SendVerificationCodeResponse(
                Success: true,
                Message: "Verification code sent successfully",
                MaskedPhoneNumber: maskedPhone,
                ExpiresIn: expiresInSeconds
            );

    }
}
