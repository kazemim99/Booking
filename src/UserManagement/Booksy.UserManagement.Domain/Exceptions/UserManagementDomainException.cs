// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.Exceptions;

namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Base exception for User Management domain
    /// </summary>
    public abstract class UserManagementDomainException : DomainException
    {
        public override string ErrorCategory => "UserManagement";

        protected UserManagementDomainException(string message) : base(message) { }

        protected UserManagementDomainException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
