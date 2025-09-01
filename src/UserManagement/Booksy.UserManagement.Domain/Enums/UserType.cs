
// ========================================
// Booksy.UserManagement.Domain/Enums/UserType.cs
// ========================================
namespace Booksy.UserManagement.Domain.Enums
{
    /// <summary>
    /// Represents the type of user in the system
    /// </summary>
    public enum UserType
    {
        /// <summary>
        /// Customer who books services
        /// </summary>
        Customer = 0,

        /// <summary>
        /// Service provider who offers services
        /// </summary>
        Provider = 1,

        /// <summary>
        /// Both customer and provider
        /// </summary>
        Both = 2,

        /// <summary>
        /// System administrator
        /// </summary>
        Admin = 3,

        /// <summary>
        /// Support staff
        /// </summary>
        Support = 4
    }
}

