// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Services;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;

namespace Booksy.UserManagement.Domain.Services
{
    /// <summary>
    /// Domain service for user validation
    /// </summary>
    public interface IUserValidationService : IDomainService
    {
        /// <summary>
        /// Validates if an email is available for registration
        /// </summary>
        Task<bool> IsEmailAvailableAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a user can be activated
        /// </summary>
        bool CanActivate(User user);

        /// <summary>
        /// Validates if a user can be suspended
        /// </summary>
        bool CanSuspend(User user);

        /// <summary>
        /// Validates if a user can be deleted
        /// </summary>
        bool CanDelete(User user);

        /// <summary>
        /// Validates user profile completeness
        /// </summary>
        bool IsProfileComplete(User user);

        /// <summary>
        /// Checks if the user needs to verify their email
        /// </summary>
        bool RequiresEmailVerification(User user);

        /// <summary>
        /// Checks if the user needs to change their password
        /// </summary>
        bool RequiresPasswordChange(User user, int maxPasswordAgeDays = 90);
    }
}


