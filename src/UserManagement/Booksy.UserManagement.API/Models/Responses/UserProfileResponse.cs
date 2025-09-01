
// ========================================
// Models/Responses/Other Responses
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string UserType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool EmailVerified { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
