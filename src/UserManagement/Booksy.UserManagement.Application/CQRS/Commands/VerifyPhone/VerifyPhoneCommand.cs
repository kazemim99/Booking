// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommand.cs
// ========================================

// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/PhoneVerification/VerifyPhone/VerifyPhoneCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.VerifyPhone
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
