// ========================================
// AsanRezerve.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Enums;
using System.Linq.Expressions;

namespace AsanRezerve.UserManagement.Domain.Specifications
{
    /// <summary>
    /// Specification for filtering active users
    /// </summary>
    public sealed class ActiveUserSpecification
    {
        public static Expression<Func<User, bool>> IsSatisfiedBy()
        {
            return user => user.Status == UserStatus.Active;
        }

        public static Expression<Func<User, bool>> IsActiveAndNotLocked()
        {
            return user => user.Status == UserStatus.Active &&
                          (user.LockedUntil == null || user.LockedUntil <= DateTime.UtcNow);
        }

        public static Expression<Func<User, bool>> IsActiveWithType(UserType type)
        {
            return user => user.Status == UserStatus.Active && user.Type == type;
        }

        public static Expression<Func<User, bool>> IsActiveInRole(string roleName)
        {
            return user => user.Status == UserStatus.Active &&
                          user.Roles.Any(r => r.Name == roleName);
        }
    }
}


