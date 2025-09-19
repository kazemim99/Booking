using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Domain.Specifications;

public sealed class UserFilterSpecification : BaseSpecification<User>
{
    public UserFilterSpecification(
        UserStatus? status = null,
        UserType? type = null,
        string? searchTerm = null)
    {
        if (status.HasValue)
            AddCriteria(user => user.Status == status.Value);

        if (type.HasValue)
            AddCriteria(user => user.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            AddCriteria(user =>
                user.Email.Value.ToLowerInvariant().Contains(term) ||
                (user.Profile != null &&
                 (user.Profile.FirstName.ToLowerInvariant().Contains(term) ||
                  user.Profile.LastName.ToLowerInvariant().Contains(term))));
        }

        // Add includes
        AddInclude(u => u.Profile);
        AddInclude(u => u.Roles);
    }

    public UserFilterSpecification WithPaging(int pageNumber, int pageSize)
    {
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        return this;
    }

    //public UserFilterSpecification OrderByEmail(bool descending = false)
    //{
    //    if (descending)
    //        ApplyOrderByDescending(u => u.Email.Value);
    //    else
    //        ApplyOrderBy(u => u.Email.Value);

    //    return this;
    //}

    //public UserFilterSpecification OrderByRegistered(bool descending = true)
    //{
    //    if (descending)
    //        ApplyOrderByDescending(u => u.RegisteredAt);
    //    else
    //        ApplyOrderBy(u => u.RegisteredAt);

    //    return this;
    //}
}
