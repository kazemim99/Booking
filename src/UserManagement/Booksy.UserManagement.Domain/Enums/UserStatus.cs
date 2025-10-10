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
        Draft = 0,

        /// <summary>
        /// User has registered but not yet activated their account
        /// </summary>
        Pending = 1,

        /// <summary>
        /// User account is active and can be used
        /// </summary>
        Active = 2,

        /// <summary>
        /// User account is temporarily suspended
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// User account is inactive (voluntarily deactivated)
        /// </summary>
        Inactive = 4,

        /// <summary>
        /// User account is banned
        /// </summary>
        Banned = 5,

        /// <summary>
        /// User account is deleted (soft delete)
        /// </summary>
        Deleted = 6
    }
}
