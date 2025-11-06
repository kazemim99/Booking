// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/VerifyPhone/VerifyPhoneResult.cs
// ========================================
namespace Booksy.UserManagement.Application.Commands.PhoneVerification.VerifyPhone
{
    /// <summary>
    /// Result of phone verification
    /// </summary>
    public sealed record VerifyPhoneResult(
        bool Success,
        string Message,
        string PhoneNumber,
        DateTime? VerifiedAt = null,
        int? RemainingAttempts = null,
        DateTime? BlockedUntil = null
    );
}
