// ========================================
// Models/Responses/SearchUsersResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response model for user search operations
/// </summary>
public class SearchUsersResponse
{
    /// <summary>
    /// Search term that was used
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Search results
    /// </summary>
    public List<UserResponse> Results { get; set; } = new();

    /// <summary>
    /// Total number of results found
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Whether search was limited to active users only
    /// </summary>
    public bool ActiveOnly { get; set; }
}
