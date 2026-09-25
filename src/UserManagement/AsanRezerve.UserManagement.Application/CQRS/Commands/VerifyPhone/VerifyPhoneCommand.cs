// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.VerifyPhone
{
    /// <summary>
    /// Command to verify phone number with OTP code
    /// </summary>
    public sealed record VerifyPhoneCommand(
        Guid VerificationId,
        string Code,
        string? IpAddress = null,
        Guid? IdempotencyKey =null
    ) : ICommand<VerifyPhoneResult>
    {
    }
}
