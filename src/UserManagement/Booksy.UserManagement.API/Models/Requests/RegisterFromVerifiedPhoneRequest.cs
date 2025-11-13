// ========================================
// Booksy.UserManagement.API/Models/Requests/RegisterFromVerifiedPhoneRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to create user account from verified phone
/// </summary>
public sealed class RegisterFromVerifiedPhoneRequest
{
    /// <summary>
    /// Phone verification ID from successful OTP verification
    /// </summary>
    [Required]
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// User type (Provider, Customer, etc.)
    /// </summary>
    [Required]
    public string UserType { get; set; } = "Provider";

    /// <summary>
    /// Optional first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Optional last name
    /// </summary>
    public string? LastName { get; set; }
}
