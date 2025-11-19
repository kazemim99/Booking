// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/ResendOtp/ResendOtpResult.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.ResendOtp
{
    /// <summary>
    /// Result of OTP resend operation
    /// </summary>
    public sealed record ResendOtpResult(
        bool Success,
        string Message,
        string PhoneNumber,
        DateTime? ExpiresAt = null,
        int? RemainingResendAttempts = null,
        DateTime? CanResendAfter = null
    );
}
