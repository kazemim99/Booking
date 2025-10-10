using Booksy.Core.Application.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

public record VerifyCodeCommand(
    string PhoneNumber,
    string Code,
    string UserType = "Provider" // Default to Provider for registration flow
) : ICommand<VerifyCodeResponse>
{
    public Guid? IdempotencyKey { get; set; }
}
