// ========================================
// Booksy.UserManagement.API/Models/Responses/ResendOtpResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after resending OTP
/// </summary>
public class ResendOtpResponse
{
    /// <summary>
    /// Whether resend was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the new code expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Remaining resend attempts
    /// </summary>
    public int? RemainingResendAttempts { get; set; }

    /// <summary>
    /// When resend is allowed again (if too soon)
    /// </summary>
    public DateTime? CanResendAfter { get; set; }
}
