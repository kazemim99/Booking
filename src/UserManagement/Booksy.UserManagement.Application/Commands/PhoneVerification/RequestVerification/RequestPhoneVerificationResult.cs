// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/RequestVerification/RequestPhoneVerificationResult.cs
// ========================================
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.RequestVerification
{
    /// <summary>
    /// Result of requesting phone verification
    /// </summary>
    public sealed record RequestPhoneVerificationResult(
        Guid VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        DateTime ExpiresAt,
        int MaxAttempts,
        string Message
    );
}
