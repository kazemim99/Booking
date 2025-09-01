// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Domain.Exceptions
{
    /// <summary>
    /// Specification for finding users by email
    /// </summary>
    public sealed class UserByEmailSpecification
    {
        public static Expression<Func<User, bool>> IsSatisfiedBy(Email email)
        {
            var emailValue = email.Value;
            return user => user.Email.Value == emailValue;
        }

        public static Expression<Func<User, bool>> EmailContains(string searchTerm)
        {
            var term = searchTerm.ToLowerInvariant();
            return user => user.Email.Value.Contains(term);
        }

        public static Expression<Func<User, bool>> EmailDomain(string domain)
        {
            var domainLower = domain.ToLowerInvariant();
            return user => user.Email == domainLower;
        }
    }
}

