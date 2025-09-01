// ========================================
// Models/Responses/UserStatisticsResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response model for user statistics
/// </summary>
public class UserStatisticsResponse
{
    /// <summary>
    /// Total number of users in the system
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of active users
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Number of pending users (not yet activated)
    /// </summary>
    public int PendingUsers { get; set; }

    /// <summary>
    /// Number of suspended users
    /// </summary>
    public int SuspendedUsers { get; set; }

    /// <summary>
    /// Number of deleted users
    /// </summary>
    public int DeletedUsers { get; set; }

    /// <summary>
    /// New users registered this week
    /// </summary>
    public int NewUsersThisWeek { get; set; }

    /// <summary>
    /// New users registered this month
    /// </summary>
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// User statistics by type
    /// </summary>
    public Dictionary<string, int> UsersByType { get; set; } = new();

    /// <summary>
    /// User registration trend (last 30 days)
    /// </summary>
    public List<DailyRegistrationCount> RegistrationTrend { get; set; } = new();
}
