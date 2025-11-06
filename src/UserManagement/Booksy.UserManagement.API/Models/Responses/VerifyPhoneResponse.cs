// ========================================
// Booksy.UserManagement.API/Models/Responses/VerifyPhoneResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after verifying phone number
/// </summary>
public class VerifyPhoneResponse
{
    /// <summary>
    /// Whether verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Verified phone number (if successful)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// When verification was completed (if successful)
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Remaining attempts (if failed)
    /// </summary>
    public int? RemainingAttempts { get; set; }

    /// <summary>
    /// When unblocked (if blocked)
    /// </summary>
    public DateTime? BlockedUntil { get; set; }
}
