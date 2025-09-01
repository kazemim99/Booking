// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetPaginatedUsers
{
    public sealed record GetPaginatedUsersQuery(
        UserStatus? Status = null,
        UserType? Type = null,
        bool SortDescending = false) : PaginationRequest, IQuery<PagedResult<GetPaginatedUsersResult>>;
}


