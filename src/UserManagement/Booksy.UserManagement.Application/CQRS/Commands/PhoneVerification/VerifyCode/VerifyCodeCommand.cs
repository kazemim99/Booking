using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

/// <summary>
/// Command to verify phone number with OTP code
/// This is a wrapper for backward compatibility - delegates to VerifyPhoneCommand
/// </summary>
public sealed record VerifyCodeCommand(
    string PhoneNumber,
    string Code,
    string UserType) : ICommand<VerifyCodeResponse>
{
    public Guid? IdempotencyKey { get; init; }
}
