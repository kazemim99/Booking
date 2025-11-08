// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a user that already exists
    /// </summary>
    public sealed class UserAlreadyExistsException : UserManagementDomainException
    {
        public override string ErrorCode => "USER_ALREADY_EXISTS";
        public Email Email { get; }

        public UserAlreadyExistsException(Email email)
            : base($"User with email '{email}' already exists")
        {
            Email = email;
            ExtensionData = new Dictionary<string, object>
            {
                ["email"] = email.Value
            };
        }

        public UserAlreadyExistsException(string message) : base(message) { }
    }
}
