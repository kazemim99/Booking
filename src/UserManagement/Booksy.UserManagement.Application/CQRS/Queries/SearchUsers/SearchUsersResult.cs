namespace Booksy.UserManagement.Application.CQRS.Queries.SearchUsers
{

    public sealed class SearchUsersResult
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public double MatchScore { get; init; }
    }
}