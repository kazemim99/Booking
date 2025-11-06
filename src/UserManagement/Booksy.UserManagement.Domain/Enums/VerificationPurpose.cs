// ========================================
// Booksy.UserManagement.Domain/Enums/VerificationPurpose.cs
// ========================================
namespace Booksy.UserManagement.Domain.Enums
{
    /// <summary>
    /// Purpose of the verification
    /// </summary>
    public enum VerificationPurpose
    {
        /// <summary>
        /// Verifying phone number during registration
        /// </summary>
        Registration,

        /// <summary>
        /// Two-factor authentication
        /// </summary>
        TwoFactorAuthentication,

        /// <summary>
        /// Password reset verification
        /// </summary>
        PasswordReset,

        /// <summary>
        /// Changing phone number
        /// </summary>
        PhoneNumberChange,

        /// <summary>
        /// Changing email address
        /// </summary>
        EmailChange,

        /// <summary>
        /// Login verification for suspicious activity
        /// </summary>
        LoginVerification,

        /// <summary>
        /// Transaction authorization
        /// </summary>
        TransactionAuthorization
    }
}
