// ========================================
// Booksy.UserManagement.Domain/Enums/VerificationStatus.cs
// ========================================
namespace Booksy.UserManagement.Domain.Enums
{
    /// <summary>
    /// Status of phone/email verification process
    /// </summary>
    public enum VerificationStatus
    {
        /// <summary>
        /// Verification requested, OTP generated
        /// </summary>
        Pending,

        /// <summary>
        /// OTP sent to user successfully
        /// </summary>
        Sent,

        /// <summary>
        /// User successfully verified the OTP
        /// </summary>
        Verified,

        /// <summary>
        /// Verification failed due to incorrect OTP
        /// </summary>
        Failed,

        /// <summary>
        /// OTP expired (default: 5 minutes)
        /// </summary>
        Expired,

        /// <summary>
        /// Too many failed attempts, blocked temporarily
        /// </summary>
        Blocked,

        /// <summary>
        /// Verification cancelled by user or system
        /// </summary>
        Cancelled
    }
}
