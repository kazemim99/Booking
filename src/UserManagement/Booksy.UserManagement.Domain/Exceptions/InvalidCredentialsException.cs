// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
// ========================================
// Booksy.UserManagement.Domain/Exceptions/InvalidCredentialsException.cs
// ========================================
using Booksy.Core.Domain.Errors;

namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication credentials are invalid
    /// </summary>
    public sealed class InvalidCredentialsException : UserManagementDomainException
    {
        public override ErrorCode ErrorCode => ErrorCode.INVALID_CREDENTIALS;
        public override int HttpStatusCode => 401;

        public InvalidCredentialsException(string message) : base(message) { }

        public InvalidCredentialsException() : base("Invalid email or password") { }
    }
}
