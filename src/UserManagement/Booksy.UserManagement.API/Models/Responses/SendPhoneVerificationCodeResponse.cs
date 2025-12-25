namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after sending verification code (API)
/// </summary>
public class SendPhoneVerificationCodeResponse
{
    /// <summary>
    /// Indicates whether the verification code was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the verification code expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
