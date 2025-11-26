namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after sending verification code
/// </summary>
public class SendVerificationCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
