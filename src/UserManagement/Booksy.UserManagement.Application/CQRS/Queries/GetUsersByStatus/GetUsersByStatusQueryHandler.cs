using Booksy.UserManagement.Application.Abstractions.Queries;
using Booksy.UserManagement.Domain.Specifications;

namespace Booksy.UserManagement.Application.Queries.GetUsersByStatus
{
    /// <summary>
    /// Handler for retrieving users filtered by status
    /// </summary>
    public sealed class GetUsersByStatusQueryHandler
        : IQueryHandler<GetUsersByStatusQuery, IReadOnlyList<GetUsersByStatusResult>>
    {
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly ILogger<GetUsersByStatusQueryHandler> _logger;

        public GetUsersByStatusQueryHandler(
            IUserQueryRepository userQueryRepository,
            ILogger<GetUsersByStatusQueryHandler> logger)
        {
            _userQueryRepository = userQueryRepository ?? throw new ArgumentNullException(nameof(userQueryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<GetUsersByStatusResult>> Handle(
            GetUsersByStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Retrieving users with status {Status}, max results: {MaxResults}",
                request.Status,
                request.MaxResults);

            try
            {
                // Create specification for filtering
                var specification = new UsersByStatusSpecification(
                    request.Status,
                    request.MaxResults);

                // Execute query with projection
                var results = await _userQueryRepository.GetListAsync(
                    specification,
                    user => new GetUsersByStatusResult
                    {
                        UserId = user.Id.Value,
                        Email = user.Email.Value,
                        FirstName = user.Profile.FirstName,
                        LastName = user.Profile.LastName,
                        FullName = user.Profile.FirstName + " " + user.Profile.LastName,
                        PhoneNumber = user.Profile.PhoneNumber != null ? user.Profile.PhoneNumber.Value : null,
                        Status = user.Status,
                        Type = user.Type,
                        Roles = user.Roles.Select(r => r.Name).ToList(),
                        RegisteredAt = user.RegisteredAt,
                        ActivatedAt = user.ActivatedAt,
                        LastLoginAt = user.LastLoginAt,
                        IsLocked = user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        AvatarUrl = user.Profile.AvatarUrl
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Successfully retrieved {UserCount} users with status {Status}",
                    results.Count,
                    request.Status);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while retrieving users with status {Status}",
                    request.Status);
                throw;
            }
        }
    }
}