// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.VerifyPhone
{
    /// <summary>
    /// Command to verify phone number with OTP code
    /// </summary>
    public sealed record VerifyPhoneCommand(
        Guid VerificationId,
        string Code,
        string? IpAddress = null
    ) : ICommand<VerifyPhoneResult>;
}
