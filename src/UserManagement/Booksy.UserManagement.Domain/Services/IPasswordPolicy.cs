// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Services;

namespace Booksy.UserManagement.Domain.Services
{
    /// <summary>
    /// Domain service for password policy validation
    /// </summary>
    public interface IPasswordPolicy : IDomainService
    {
        /// <summary>
        /// Validates a password against the policy
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <returns>A collection of validation errors, empty if valid</returns>
        IEnumerable<string> Validate(string password);

        /// <summary>
        /// Checks if a password meets the minimum requirements
        /// </summary>
        bool IsValid(string password);

        /// <summary>
        /// Gets the password policy description
        /// </summary>
        string GetPolicyDescription();

        /// <summary>
        /// Checks if a password has been compromised (using HaveIBeenPwned or similar)
        /// </summary>
        Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a strong random password
        /// </summary>
        string GenerateStrongPassword(int length = 16);
    }
}


