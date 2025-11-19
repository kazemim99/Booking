// ========================================
// Booksy.UserManagement.Application/Queries/SearchUsers/SearchUsersResult.cs
// ========================================
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.SearchUsers;

/// <summary>
/// Simplified result model for user search operations
/// </summary>
public sealed class SearchUsersResult
{
    /// <summary>
    /// User unique identifier
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name (computed)
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Current user status
    /// </summary>
    public UserStatus Status { get; set; }

    /// <summary>
    /// User type
    /// </summary>
    public UserType Type { get; set; }

    /// <summary>
    /// Whether the account is currently locked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether two-factor authentication is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// When the user registered
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// When the user account was activated
    /// </summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// List of user roles (if requested)
    /// </summary>
    public List<UserRoleInfo> Roles { get; set; } = new();

    /// <summary>
    /// Primary role (most recent active role)
    /// </summary>
    public string? PrimaryRole { get; set; }

    /// <summary>
    /// User display name (full name or email if no name)
    /// </summary>
    public string DisplayName
    {
        get
        {
            var fullName = FullName;
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : Email;
        }
    }

    /// <summary>
    /// Account status description
    /// </summary>
    public string StatusDescription => Status switch
    {
        UserStatus.Pending => "Pending Activation",
        UserStatus.Active => IsLocked ? "Active (Locked)" : "Active",
        UserStatus.Inactive => "Inactive",
        UserStatus.Suspended => "Suspended",
        UserStatus.Deleted => "Deleted",
        _ => Status.ToString()
    };
}

/// <summary>
/// Simplified role information for search results
/// </summary>
public sealed class UserRoleInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
}