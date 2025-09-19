
// ========================================

namespace Booksy.UserManagement.API.Models.Responses;

public class UserDetailsResponse : UserResponse
{
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool EmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
