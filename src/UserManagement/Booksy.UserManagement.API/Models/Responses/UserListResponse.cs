// ========================================
// Models/Responses/UserListResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response model for user list operations
/// </summary>
public class UserListResponse
{
    /// <summary>
    /// List of users
    /// </summary>
    public List<UserResponse> Users { get; set; } = new();

    /// <summary>
    /// Total number of users matching the criteria
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Status filter applied (if any)
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
