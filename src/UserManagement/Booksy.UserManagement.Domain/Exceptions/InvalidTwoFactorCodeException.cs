
// ========================================
// Booksy.UserManagement.Domain/Exceptions/InvalidTwoFactorCodeException.cs
// ========================================
using Booksy.Core.Domain.Errors;

namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when two-factor authentication code is invalid
    /// </summary>
    public sealed class InvalidTwoFactorCodeException : UserManagementDomainException
    {
        public override ErrorCode ErrorCode => ErrorCode.INVALID_TWO_FACTOR_CODE;
        public override int HttpStatusCode => 401;

        public InvalidTwoFactorCodeException()
            : base("Invalid two-factor authentication code") { }

        public InvalidTwoFactorCodeException(string message) : base(message) { }
    }
}

