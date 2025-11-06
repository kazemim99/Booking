// ========================================
// Booksy.UserManagement.Domain/Enums/VerificationMethod.cs
// ========================================
namespace Booksy.UserManagement.Domain.Enums
{
    /// <summary>
    /// Method used for verification
    /// </summary>
    public enum VerificationMethod
    {
        /// <summary>
        /// Verification via SMS to phone number
        /// </summary>
        Sms,

        /// <summary>
        /// Verification via email (fallback)
        /// </summary>
        Email,

        /// <summary>
        /// Verification via voice call (future)
        /// </summary>
        VoiceCall,

        /// <summary>
        /// Verification via authenticator app (future)
        /// </summary>
        AuthenticatorApp
    }
}
