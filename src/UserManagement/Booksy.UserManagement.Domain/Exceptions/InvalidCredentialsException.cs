// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
// ========================================
// Booksy.UserManagement.Domain/Exceptions/InvalidCredentialsException.cs
// ========================================
namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication credentials are invalid
    /// </summary>
    public sealed class InvalidCredentialsException : UserManagementDomainException
    {
        public override string ErrorCode => "INVALID_CREDENTIALS";
        public override int HttpStatusCode => 401;

        public InvalidCredentialsException(string message) : base(message) { }

        public InvalidCredentialsException() : base("Invalid email or password") { }
    }
}
