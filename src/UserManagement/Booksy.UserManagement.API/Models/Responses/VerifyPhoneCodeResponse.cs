namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after verifying phone code
/// </summary>
public class VerifyPhoneCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
