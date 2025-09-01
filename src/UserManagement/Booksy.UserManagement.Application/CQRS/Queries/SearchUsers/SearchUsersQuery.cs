// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Queries.SearchUsers
{
    public sealed record SearchUsersQuery(
        string SearchTerm,
        bool ActiveOnly = true) :PaginationRequest, IQuery<PagedResult<SearchUsersResult>>;
}
