using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to send phone verification code
/// </summary>
public class SendPhoneVerificationRequest
{
    /// <summary>
    /// Phone number to verify (Iranian format: 09XXXXXXXXX)
    /// </summary>
    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با 09 شروع شود و 11 رقم باشد")]
    public string PhoneNumber { get; set; } = string.Empty;
}
