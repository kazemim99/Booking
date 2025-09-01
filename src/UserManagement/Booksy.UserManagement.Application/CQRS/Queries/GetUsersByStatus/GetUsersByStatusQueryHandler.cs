// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================

using Booksy.Core.Application.Extensions;


namespace Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    public sealed class GetUsersByStatusQueryHandler : IQueryHandler<GetUsersByStatusQuery, PagedResult<GetUsersByStatusResult>>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<GetUsersByStatusQueryHandler> _logger;

        public GetUsersByStatusQueryHandler(
            IUserReadRepository userReadRepository,
            ILogger<GetUsersByStatusQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }

        public async Task<PagedResult<GetUsersByStatusResult>> Handle(
            GetUsersByStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Fetching users with status: {Status}", request.Status);

            var users = _userReadRepository.GetQueryable().Where(c=>c.Status == request.Status);

            return await users.Select(user => new GetUsersByStatusResult
            {
                UserId = user.Id.Value,
                Email = user.Email.Value,
                FullName = user.Profile.GetFullName(),
                DisplayName = user.Profile.GetDisplayName(),
                Status = user.Status.ToString(),
                Type = user.Type.ToString(),
                RegisteredAt = user.RegisteredAt,
                LastLoginAt = user.LastLoginAt,
                IsLocked = user.IsLocked(),
                AvatarUrl = user.Profile.AvatarUrl
            }).ToPagedResultAsync(request,cancellationToken);

        }
    }
}
