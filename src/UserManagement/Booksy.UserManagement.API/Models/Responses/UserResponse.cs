// ========================================
// Models/Responses/UserResponse.cs
// ========================================

using Booksy.UserManagement.Domain.Entities;

namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response model for user information
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public List<string> Roles { get; set; }
    public List<string>? Permissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public UserProfileResponse Profile { get; set; }

}

