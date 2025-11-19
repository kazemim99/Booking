using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.SendVerificationCode;

/// <summary>
/// Command to send verification code to phone number
/// This is a wrapper for backward compatibility - delegates to RequestPhoneVerificationCommand
/// </summary>
public sealed record SendVerificationCodeCommand(
    string PhoneNumber,
    string CountryCode,
    string? IpAddress,
    string? UserAgent) : ICommand<SendVerificationCodeResponse>
{
    public Guid? IdempotencyKey { get; init; }
}
