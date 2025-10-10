using Booksy.Core.Application.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;

public record SendVerificationCodeCommand(
    string PhoneNumber,
    string CountryCode = "+98",
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<SendVerificationCodeResponse>
{
    public Guid? IdempotencyKey { get; set; }
}
