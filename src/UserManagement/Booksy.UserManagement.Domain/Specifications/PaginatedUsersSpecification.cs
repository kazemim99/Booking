// 📁 Booksy.UserManagement.Domain/Specifications/PaginatedUsersSpecification.cs
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Domain.Specifications
{
    /// <summary>
    /// Specification for paginated user filtering with sorting and search
    /// </summary>
    public sealed class PaginatedUsersSpecification : BaseSpecification<User>
    {
        public PaginatedUsersSpecification(
            UserStatus? status = null,
            UserType? type = null,
            string? searchTerm = null,
            int pageNumber = 1,
            int pageSize = 20,
            string sortBy = "RegisteredAt",
            bool sortDescending = true)
        {
            // ✅ FIX: Build criteria step by step using AddCriteria method

            // Status filter
            if (status.HasValue)
            {
                AddCriteria(user => user.Status == status.Value);
            }

            // Type filter
            if (type.HasValue)
            {
                AddCriteria(user => user.Type == type.Value);
            }

            // Search term filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(user =>
                    user.Email.Value.ToLower().Contains(term) ||
                    user.Profile.FirstName.ToLower().Contains(term) ||
                    user.Profile.LastName.ToLower().Contains(term));
            }

            // Exclude deleted users by default unless specifically requested
            if (status != UserStatus.Deleted)
            {
                AddCriteria(user => user.Status != UserStatus.Deleted);
            }

            // Add necessary includes
            AddInclude(u => u.Profile);
            AddInclude(u => u.Roles);

            // Apply sorting
            //ApplySorting(sortBy, sortDescending);

            // Apply pagination
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }

        //private void ApplySorting(string sortBy, bool descending)
        //{
        //    switch (sortBy.ToLowerInvariant())
        //    {
        //        case "email":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.Email.Value);
        //            else
        //                ApplyOrderBy(u => u.Email.Value);
        //            break;

        //        case "firstname":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.Profile.FirstName);
        //            else
        //                ApplyOrderBy(u => u.Profile.FirstName);
        //            break;

        //        case "lastname":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.Profile.LastName);
        //            else
        //                ApplyOrderBy(u => u.Profile.LastName);
        //            break;

        //        case "status":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.Status);
        //            else
        //                ApplyOrderBy(u => u.Status);
        //            break;

        //        case "type":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.Type);
        //            else
        //                ApplyOrderBy(u => u.Type);
        //            break;

        //        case "activatedat":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.ActivatedAt);
        //            else
        //                ApplyOrderBy(u => u.ActivatedAt);
        //            break;

        //        case "lastloginat":
        //            if (descending)
        //                ApplyOrderByDescending(u => u.LastLoginAt);
        //            else
        //                ApplyOrderBy(u => u.LastLoginAt);
        //            break;

        //        case "registeredat":
        //        default:
        //            if (descending)
        //                ApplyOrderByDescending(u => u.RegisteredAt);
        //            else
        //                ApplyOrderBy(u => u.RegisteredAt);
        //            break;
        //    }

        //    // Add secondary sort by ID for consistent ordering
        //    ApplyThenBy(u => u.Id.Value);
        //}
    }
}