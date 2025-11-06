using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Application.Commands.PhoneVerification.RequestVerification;
using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;

/// <summary>
/// Handler for SendVerificationCode command - wrapper for backward compatibility
/// Delegates to RequestPhoneVerificationCommand
/// </summary>
public sealed class SendVerificationCodeCommandHandler
    : ICommandHandler<SendVerificationCodeCommand, SendVerificationCodeResponse>
{
    private readonly ISender _mediator;

    public SendVerificationCodeCommandHandler(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task<SendVerificationCodeResponse> Handle(
        SendVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        // Delegate to the new command
        var newCommand = new RequestPhoneVerificationCommand(
            PhoneNumber: request.PhoneNumber,
            CountryCode: request.CountryCode);

        var result = await _mediator.Send(newCommand, cancellationToken);

        // Map to the old response format
        return new SendVerificationCodeResponse(
            VerificationId: result.VerificationId,
            MaskedPhoneNumber: MaskPhoneNumber(result.PhoneNumber),
            ExpiresAt: result.ExpiresAt,
            MaxAttempts: result.MaxAttempts,
            Message: result.Message
        );
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return phoneNumber;

        var lastDigits = phoneNumber.Substring(phoneNumber.Length - 4);
        return $"•••{lastDigits}";
    }
}
