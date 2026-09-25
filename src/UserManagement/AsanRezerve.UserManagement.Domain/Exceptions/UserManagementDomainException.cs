// ========================================
// AsanRezerve.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using AsanRezerve.Core.Domain.Exceptions;

namespace AsanRezerve.UserManagement.Domain.Exceptions
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
