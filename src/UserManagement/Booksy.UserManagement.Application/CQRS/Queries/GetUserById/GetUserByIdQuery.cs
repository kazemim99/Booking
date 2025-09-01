// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDetailsViewModel>
    {
        public bool IsCacheable => true;
        public string CacheKey => $"user:details:{UserId}";
        public int CacheExpirationSeconds => 300; // 5 minutes
    }
}

