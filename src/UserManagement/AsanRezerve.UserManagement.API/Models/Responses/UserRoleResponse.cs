
#region Response Models

/// <summary>
/// Response model for user search results
/// </summary>

/// <summary>
/// Role information in search response
/// </summary>
public class UserRoleResponse
{
    public string Name { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

#endregion