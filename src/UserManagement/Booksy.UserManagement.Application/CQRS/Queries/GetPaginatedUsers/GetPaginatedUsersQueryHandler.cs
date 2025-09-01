// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================

using Booksy.Core.Application.Extensions;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetPaginatedUsers
{
    public sealed class GetPaginatedUsersQueryHandler : IQueryHandler<GetPaginatedUsersQuery, PagedResult<GetPaginatedUsersResult>>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<GetPaginatedUsersQueryHandler> _logger;

        public GetPaginatedUsersQueryHandler(
            IUserReadRepository userReadRepository,
            ILogger<GetPaginatedUsersQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }

        public async Task<PagedResult<GetPaginatedUsersResult>> Handle(
            GetPaginatedUsersQuery request,
            CancellationToken cancellationToken)
        {

            var result = await _userReadRepository.GetQueryable()
                .Where(c => request.Status == null || c.Status == request.Status)
               .Select(user => new GetPaginatedUsersResult
               {
                   UserId = user.Id.Value,
                   Email = user.Email.Value,
                   FirstName = user.Profile.FirstName,
                   LastName = user.Profile.LastName,
                   FullName = user.Profile.GetFullName(),
                   PhoneNumber = user.Profile.PhoneNumber.Value,
                   Status = user.Status,
                   Type = user.Type,
                   Roles = user.Roles.Select(r => r.Name).ToList(),
                   RegisteredAt = user.RegisteredAt,
                   ActivatedAt = user.ActivatedAt,
                   LastLoginAt = user.LastLoginAt,
                   IsLocked = user.IsLocked(),
                   TwoFactorEnabled = user.TwoFactorEnabled,
                   AvatarUrl = user.Profile.AvatarUrl
               }).ToPagedResultAsync(request);


            return result;

        }
    }
}


