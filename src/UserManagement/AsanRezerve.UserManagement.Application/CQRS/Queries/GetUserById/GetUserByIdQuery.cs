// ========================================
// AsanRezerve.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
namespace AsanRezerve.UserManagement.Application.CQRS.Queries.GetUserById
{
    // Not cacheable: personal data its owner edits, and nothing evicted it (add-observability-and-caching).
    public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDetailsViewModel>;
}

