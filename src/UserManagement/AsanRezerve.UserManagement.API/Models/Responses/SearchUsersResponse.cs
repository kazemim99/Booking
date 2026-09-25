
#region Response Models

/// <summary>
/// Response model for user search results
/// </summary>
public class SearchUsersResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public List<UserRoleResponse> Roles { get; set; } = new();
    public string? PrimaryRole { get; set; }
}

#endregion