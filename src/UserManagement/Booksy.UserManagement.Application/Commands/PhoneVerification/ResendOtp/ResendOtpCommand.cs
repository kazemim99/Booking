// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/ResendOtp/ResendOtpCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.ResendOtp
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
