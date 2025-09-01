
// ========================================

namespace Booksy.UserManagement.API.Models.Responses;

public class AuthenticationResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserInfoResponse UserInfo { get; set; } = new();
}



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

public class MessageResponse
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }

    public MessageResponse(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}
