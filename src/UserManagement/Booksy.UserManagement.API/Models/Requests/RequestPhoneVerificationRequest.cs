// ========================================
// Booksy.UserManagement.API/Models/Requests/RequestPhoneVerificationRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to initiate phone verification
/// </summary>
public class RequestPhoneVerificationRequest
{
    /// <summary>
    /// Phone number to verify (supports formats: 09xxxxxxxxx, +989xxxxxxxxx, etc.)
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Verification method (SMS, Call, WhatsApp)
    /// </summary>
    [Required(ErrorMessage = "Method is required")]
    public string Method { get; set; } = "SMS";

    /// <summary>
    /// Purpose of verification (Registration, Login, PasswordReset, PhoneChange)
    /// </summary>
    [Required(ErrorMessage = "Purpose is required")]
    public string Purpose { get; set; } = "Registration";

    /// <summary>
    /// Optional user ID if associated with existing user
    /// </summary>
    public Guid? UserId { get; set; }
}
