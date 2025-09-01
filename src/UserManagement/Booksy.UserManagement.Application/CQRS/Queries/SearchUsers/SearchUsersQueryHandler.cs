// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
using Booksy.Core.Application.Extensions;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.SearchUsers
{
    public sealed class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, PagedResult<SearchUsersResult>>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<SearchUsersQueryHandler> _logger;

        public SearchUsersQueryHandler(
            IUserReadRepository userReadRepository,
            ILogger<SearchUsersQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }

        public async Task<PagedResult<SearchUsersResult>> Handle(
            SearchUsersQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Searching users with term: {SearchTerm}", request.SearchTerm);

            var users = await _userReadRepository.SearchAsync(
                request.SearchTerm,
                cancellationToken);

            // Filter active only if requested
            if (request.ActiveOnly)
            {
                users = users.Where(u => u.Status == UserStatus.Active);
            }

            var searchResults =await users.Select(user => new SearchUsersResult
            {
                UserId = user.Id.Value,
                Email = user.Email.Value,
                FullName = user.Profile.GetFullName(),
                DisplayName = user.Profile.GetDisplayName(),
                PhoneNumber = user.Profile.PhoneNumber.Value,
                Status = user.Status.ToString(),
                Type = user.Type.ToString(),
                AvatarUrl = user.Profile.AvatarUrl,
                MatchScore = CalculateMatchScore(user, request.SearchTerm)
            }).ToPagedResultAsync(request);

            return searchResults;
        }

        private double CalculateMatchScore(
            User user,
            string searchTerm)
        {
            var term = searchTerm.ToLowerInvariant();
            double score = 0;

            // Exact email match gets highest score
            if (user.Email.Value.Equals(term, StringComparison.OrdinalIgnoreCase))
                return 100;

            // Email contains term
            if (user.Email.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 50;

            // Full name exact match
            var fullName = user.Profile.GetFullName().ToLowerInvariant();
            if (fullName.Equals(term))
                score += 80;
            else if (fullName.Contains(term))
                score += 30;

            // First or last name matches
            if (user.Profile.FirstName.Equals(term, StringComparison.OrdinalIgnoreCase))
                score += 40;
            else if (user.Profile.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (user.Profile.LastName.Equals(term, StringComparison.OrdinalIgnoreCase))
                score += 40;
            else if (user.Profile.LastName.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 20;

            // Phone number contains term
            if (user.Profile.PhoneNumber != null &&
                user.Profile.PhoneNumber.Value.Contains(term))
                score += 15;

            return Math.Min(score, 100);
        }
    }
}