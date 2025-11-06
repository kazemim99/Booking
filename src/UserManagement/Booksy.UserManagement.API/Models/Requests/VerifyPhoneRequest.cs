// ========================================
// Booksy.UserManagement.API/Models/Requests/VerifyPhoneRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to verify phone number with OTP code
/// </summary>
public class VerifyPhoneRequest
{
    /// <summary>
    /// Verification ID received from request endpoint
    /// </summary>
    [Required(ErrorMessage = "Verification ID is required")]
    public Guid VerificationId { get; set; }

    /// <summary>
    /// OTP code received via SMS/Call
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
    public string Code { get; set; } = string.Empty;
}
