// ========================================
// AsanRezerve.UserManagement.Application/Commands/PhoneVerification/ResendOtp/ResendOtpCommand.cs
// ========================================

// ========================================
// AsanRezerve.UserManagement.Application/Commands/PhoneVerification/ResendOtp/ResendOtpCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.ResendOtp
{
    /// <summary>
    /// Command to resend OTP code for phone verification
    /// </summary>
    public sealed record ResendOtpCommand(
        Guid VerificationId,
        string? IpAddress = null,
        string? UserAgent = null,
        string? SessionId = null,
        Guid? IdempotencyKey = null
    ) : ICommand<ResendOtpResult>;
}
