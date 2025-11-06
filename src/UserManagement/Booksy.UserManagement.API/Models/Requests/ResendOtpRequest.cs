// ========================================
// Booksy.UserManagement.API/Models/Requests/ResendOtpRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to resend OTP code
/// </summary>
public class ResendOtpRequest
{
    /// <summary>
    /// Verification ID to resend OTP for
    /// </summary>
    [Required(ErrorMessage = "Verification ID is required")]
    public Guid VerificationId { get; set; }
}
