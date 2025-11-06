// ========================================
// Booksy.UserManagement.API/Models/Responses/PhoneVerificationResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response after requesting phone verification
/// </summary>
public class PhoneVerificationResponse
{
    /// <summary>
    /// Unique verification identifier
    /// </summary>
    public Guid VerificationId { get; set; }

    /// <summary>
    /// Phone number being verified (masked for security)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Verification method used
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// When the code expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Maximum attempts allowed
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
