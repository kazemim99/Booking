// 📁 Booksy.UserManagement.Domain/Specifications/UsersByStatusSpecification.cs
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Specifications
{
    /// <summary>
    /// Specification for filtering users by status
    /// </summary>
    public sealed class UsersByStatusSpecification : BaseSpecification<User>
    {
        public UsersByStatusSpecification(UserStatus status, int? maxResults = null)
        {
            // Apply status filter
            ApplyCriteria(user => user.Status == status);

            // Add necessary includes for complete user data
            AddInclude(u => u.Profile);
            AddInclude(u => u.Roles);

            // Apply ordering - most recently registered first
            //ApplyOrderByDescending(u => u.RegisteredAt);

            // Apply limit if specified
            if (maxResults.HasValue && maxResults.Value > 0)
            {
                ApplyPaging(0, maxResults.Value);
            }
        }

        /// <summary>
        /// Factory method for active users
        /// </summary>
        public static UsersByStatusSpecification Active(int? maxResults = null) =>
            new(UserStatus.Active, maxResults);

        /// <summary>
        /// Factory method for inactive users
        /// </summary>
        public static UsersByStatusSpecification Inactive(int? maxResults = null) =>
            new(UserStatus.Inactive, maxResults);

        /// <summary>
        /// Factory method for pending users
        /// </summary>
        public static UsersByStatusSpecification Pending(int? maxResults = null) =>
            new(UserStatus.Pending, maxResults);

        /// <summary>
        /// Factory method for deleted users
        /// </summary>
        public static UsersByStatusSpecification Deleted(int? maxResults = null) =>
            new(UserStatus.Deleted, maxResults);
    }
}