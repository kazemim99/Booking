// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
namespace Booksy.UserManagement.Domain.Enums
{
    /// <summary>
    /// Represents the status of a user account
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// User has registered but not yet activated their account
        /// </summary>
        Pending = 0,

        /// <summary>
        /// User account is active and can be used
        /// </summary>
        Active = 1,

        /// <summary>
        /// User account is temporarily suspended
        /// </summary>
        Suspended = 2,

        /// <summary>
        /// User account is inactive (voluntarily deactivated)
        /// </summary>
        Inactive = 3,

        /// <summary>
        /// User account is banned
        /// </summary>
        Banned = 4,

        /// <summary>
        /// User account is deleted (soft delete)
        /// </summary>
        Deleted = 5
    }
}
