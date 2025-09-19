// ========================================
// Models/Responses/UserResponse.cs
// ========================================

namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response model for user information
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

