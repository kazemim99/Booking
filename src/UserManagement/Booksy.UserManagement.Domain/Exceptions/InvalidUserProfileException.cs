﻿// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
// ========================================
// Booksy.UserManagement.Domain/Exceptions/InvalidUserProfileException.cs
// ========================================
namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when user profile data is invalid
    /// </summary>
    public sealed class InvalidUserProfileException : UserManagementDomainException
    {
        public override string ErrorCode => "INVALID_USER_PROFILE";

        public InvalidUserProfileException(string message) : base(message) { }

        public InvalidUserProfileException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

