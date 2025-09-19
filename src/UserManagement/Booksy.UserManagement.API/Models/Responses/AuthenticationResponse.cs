
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
