// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
using Booksy.UserManagement.Application.CQRS.Queries.GetPaginatedUsers;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    public sealed record GetUsersByStatusQuery(
        UserStatus Status) :PaginationRequest, IQuery<PagedResult<GetUsersByStatusResult>>;
}

